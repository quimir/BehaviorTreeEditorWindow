using System;
using System.IO;
using ExTools.Utillties;
using UnityEngine;

namespace LogManager.LogConfigurationManager
{
    /// <summary>
    /// Provides a base implementation for managing configuration settings related to logging operations.
    /// This abstract class defines the structure for loading, saving, serializing, and deserializing
    /// log configuration objects, as well as handling configuration changes.
    /// </summary>
    public abstract class BaseConfigurationManager : IConfigurationManager<LogConfiguration>
    {
        private readonly string default_file_name_;
        private readonly string default_directory_;

        private LogConfiguration configuration_;

        public LogConfiguration ManagerConfiguration
        {
            get => configuration_;
            set
            {
                bool changed = configuration_ == value || !AreConfigurationsEqual(configuration_, value);
                configuration_ = value;
                
                // 立即触发处理事件
                if (changed)
                {
                    OnConfigurationChanged(configuration_);
                }
            }
        }

        /// <summary>
        /// A delegate used to handle configuration changes, receiving the updated log configuration
        /// as a parameter when the change occurs.
        /// </summary>
        /// <param name="new_configuration">The updated log configuration instance.</param>
        public delegate void ConfigurationChangedHandler(LogConfiguration new_configuration);

        /// <summary>
        /// Event triggered when the logging configuration settings are updated or changed.
        /// Subscribed handlers receive the new configuration as a parameter to take appropriate action.
        /// </summary>
        public event ConfigurationChangedHandler ConfigurationChanged;

        /// <summary>
        /// Represents an abstract base class for managing configuration objects of type <see cref="LogConfiguration"/>.
        /// Provides common functionality for loading, saving, serializing, and deserializing configurations.
        /// </summary>
        /// <remarks>This class should be inherited to implement specific serialization and deserialization mechanisms.</remarks>
        protected BaseConfigurationManager(LogConfiguration configuration = null,
            string default_file_name = FixedValues.kLogConfigurationFileName, string base_directory = null)
        {
            if (string.IsNullOrEmpty(default_file_name))
            {
                throw new ArgumentNullException(nameof(default_file_name));
            }

            default_file_name_ = default_file_name;
            default_directory_ = base_directory ?? Application.persistentDataPath;
            
            if (default_file_name_.IndexOf(Path.DirectorySeparatorChar) != -1 || default_file_name_.IndexOf(Path.AltDirectorySeparatorChar) != -1)
            {
                default_file_name_ = Path.GetFileName(default_file_name_);
            }
            

            ManagerConfiguration = (configuration ?? Load(null)) ?? new LogConfiguration();
        }
        
        
        public LogConfiguration Load(string file_path, bool create_default_missing = true)
        {
            var path=file_path??GetDefaultPath();

            if (!File.Exists(path))
            {
                if (!create_default_missing) throw new FileNotFoundException();
                
                LogConfiguration default_config = new LogConfiguration();
                    
                if (Save(default_config,path))
                {
                    return default_config;
                }
            }

            var data = File.ReadAllBytes(path);
            return Deserialize(data);
        }

        public bool Save(LogConfiguration configuration, string file_path)
        {
            var path=file_path??GetDefaultPath();

            try
            {
                string directory=Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    if (directory!=null)
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    var data=Serialize(configuration);
                    File.WriteAllBytes(path, data);
                    return true;
                }
                else
                {
                    var data=Serialize(configuration);
                    File.WriteAllBytes(path, data);
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving configuration to {path}: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        public virtual string GetDefaultPath()
        {
            return GetActualPath();
        }

        /// <summary>
        /// Invoked when the configuration has been updated or changed. This method triggers the
        /// <see cref="ConfigurationChanged"/> event to notify subscribers of the updated configuration.
        /// </summary>
        /// <param name="new_configuration">
        /// The new <see cref="LogConfiguration"/> object that represents the updated configuration settings.
        /// </param>
        protected virtual void OnConfigurationChanged(LogConfiguration new_configuration)
        {
            ConfigurationChanged?.Invoke(new_configuration);
        }

        /// <summary>
        /// Determines whether two <see cref="LogConfiguration"/> objects are equal by comparing their key properties.
        /// </summary>
        /// <param name="c1">The first <see cref="LogConfiguration"/> object to compare.</param>
        /// <param name="c2">The second <see cref="LogConfiguration"/> object to compare.</param>
        /// <returns>
        /// True if both configurations have the same values for the relevant properties or if both are null;
        /// otherwise, false.
        /// </returns>
        private bool AreConfigurationsEqual(LogConfiguration c1, LogConfiguration c2)
        {
            if (c1 == null || c2 == null)
            {
                return c1 == c2;
            }
            
            // 比较关心字段
            return c1.LogDirectory == c2.LogDirectory && c1.EnableRotation == c2.EnableRotation &&
                   c1.RetentionDays == c2.RetentionDays &&
                   c1.EnableRotation == c2.EnableRotation && c1.LogWriteMode == c2.LogWriteMode &&
                   Mathf.Approximately(c1.MaxFileSizeMB, c2.MaxFileSizeMB);
        }

        /// <summary>
        /// Resolves the actual file path based on the provided file name or path.
        /// Combines the default directory path with the file name if a relative path is provided,
        /// and ensures a valid absolute path is returned.
        /// </summary>
        /// <param name="file_path">
        /// The file path specified by the user. If null or empty, the default path is returned.
        /// If a file name is provided without being an absolute path, it combines it with the default directory.
        /// </param>
        /// <returns>
        /// A fully resolved file path as a string. If no file path is provided, the default file path is returned.
        /// </returns>
        private string GetActualPath(string file_path = null)
        {
            if (!string.IsNullOrEmpty(file_path))
            {
                // 如果提供了文件名但不是绝对路径，则组合默认目录和文件名
                if (!Path.IsPathRooted(file_path) && !string.IsNullOrEmpty(Path.GetFileName(file_path)))
                {
                    return Path.Combine(default_directory_, Path.GetFileName(file_path));
                }
                // 否则，假定是完整路径（可能是绝对路径或相对当前工作目录的路径）
                return file_path;

            }
            // 如果 file_path 为空，返回默认文件路径
            return Path.Combine(default_directory_, default_file_name_);
        }

        /// <summary>
        /// Deserializes a byte array into a <see cref="LogConfiguration"/> object.
        /// This method is responsible for converting the binary representation of the configuration
        /// back into a <see cref="LogConfiguration"/> instance for further use.
        /// </summary>
        /// <param name="data">The byte array representing the serialized configuration data.</param>
        /// <returns>A <see cref="LogConfiguration"/> object deserialized from the provided byte array.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if the input <paramref name="data"/> is null.
        /// </exception>
        /// <exception cref="System.FormatException">
        /// Thrown if the input <paramref name="data"/> does not represent a valid configuration object.
        /// </exception>
        protected abstract LogConfiguration Deserialize(byte[] data);

        /// <summary>
        /// Serializes a log configuration object into a byte array format suitable for storage or transmission.
        /// </summary>
        /// <param name="configuration">The log configuration object to be serialized.</param>
        /// <returns>A byte array representing the serialized log configuration.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when the <paramref name="configuration"/> parameter is null.
        /// </exception>
        protected abstract byte[] Serialize(LogConfiguration configuration);
    }
}