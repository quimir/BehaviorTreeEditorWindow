using System;
using System.IO;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogManagerFactory;
using Save.Serialization.Core;
using Save.Serialization.Core.FileStorage;
using Save.Serialization.Core.TypeConverter;
using Save.Serialization.Factory;
using UnityEngine;

namespace Save.FileStorage
{
    /// <summary>
    /// Manages the saving and loading of file paths using a specified serializer type.
    /// </summary>
    public class FileRecordManager : SingletonWithLazy<FileRecordManager>, IDisposable
    {
        /// <summary>
        /// Gets the instance of <see cref="FileRecordStorage"/> used for managing file path records.
        /// This property provides access to the deserialized storage of file records
        /// and initializes a new instance of <see cref="FileRecordStorage"/> if the
        /// deserialization process fails or returns null.
        /// </summary>
        public FileRecordStorage FilePathStorage { get; private set; }

        private ISerializer serializer_;

        private string FilePathStorageFilePath = FixedValues.kDefaultFileStoragePath;

        /// <summary>
        /// Specifies the type of serializer to use for managing file path records.
        /// This variable determines the format for serializing and deserializing file storage data.
        /// Default value is set to <see cref="SerializerType.kJson"/>.
        /// </summary>
        public static SerializerType SerializerType = SerializerType.kJson;

        private static LogSpaceNode log_space_ => new("FileRecordManager");

        protected override void InitializationInternal()
        {
            LoadFilePathStorage(SerializerType);
        }

        private void LoadFilePathStorage(SerializerType type = SerializerType.kJson)
        {
            serializer_ = SerializerFactory.Instance.CreateSerializer(type, new SerializationSettings
            {
                PreserveReferences = true,
                TypeNameHandling = SerializationTypeNameHandling.kAuto,
                PrettyPrint = true
            });

            if (serializer_ is ISerializerFileStorage storage)
            {
                var filePath = Path.Combine(Application.persistentDataPath, FilePathStorageFilePath);
                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"Loading File Record Storage from: {filePath}"));

                try
                {
                    // 1. 将加载结果存到一个临时变量中
                    var loadedStorage = storage.LoadFromFile<FileRecordStorage>(filePath);

                    // 2. 严格检查加载结果
                    if (loadedStorage == null)
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                            new LogEntry(LogLevel.kError, "Deserialization FAILED! LoadFromFile returned null."), true);
                        FilePathStorage = new FileRecordStorage();
                    }
                    else
                    {
                        ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                            new LogEntry(LogLevel.kInfo,
                                "Deserialization SUCCEEDED. Records count: " + loadedStorage.FileRecords.Count));
                        FilePathStorage = loadedStorage;
                    }
                }
                catch (Exception e)
                {
                    ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                        new LogEntry(LogLevel.kError,
                            $"An exception occurred during deserialization: {e.Message}\n{e.StackTrace}"), true);
                    FilePathStorage = new FileRecordStorage();
                }

                ViewLogManagerFactory.Instance.TryGetLogWriter(FixedValues.kDefaultLogSpace).AddLog(log_space_,
                    new LogEntry(LogLevel.kInfo, $"Loading File Record Storage finished."));
                return;
            }

            FilePathStorage ??= new FileRecordStorage();
        }

        /// <summary>
        /// Saves the file path storage to a specified location.
        /// </summary>
        /// <param name="file_path">The file path to save the storage to. If not provided, the default file storage
        /// path is used.</param>
        public void SaveFilePathStorage(string file_path = null)
        {
            if (string.IsNullOrEmpty(file_path)) file_path = FilePathStorageFilePath;

            if (serializer_ == null)
                serializer_ = SerializerCreator.Instance.Create(SerializerType, new SerializationSettings
                {
                    PreserveReferences = true,
                    TypeNameHandling = SerializationTypeNameHandling.kAuto,
                    PrettyPrint = true
                });

            if (!FilePathStorageFilePath.Equals(file_path)) FilePathStorageFilePath = file_path;


            if (serializer_ is ISerializerFileStorage storage)
                storage.SaveToFile(FilePathStorage, Path.Combine(Application.persistentDataPath, file_path));

            return;
        }

        public void Dispose()
        {
            SaveFilePathStorage(FilePathStorageFilePath);
        }
    }
}