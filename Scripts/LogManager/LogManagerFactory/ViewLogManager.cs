using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ExTools.Singleton;
using ExTools.Utillties;
using LogManager.Core;
using LogManager.LogConfigurationManager;
using LogManager.Storage;
using UnityEditor;
using UnityEngine;

namespace LogManager.LogManagerFactory
{
    /// <summary>
    /// A factory class responsible for managing instances of log writers and their configurations.
    /// Provides functionalities for registering, retrieving, and managing log writers
    /// based on the provided configuration, utilizing lazy initialization.
    /// </summary>
    public class ViewLogManagerFactory : SingletonWithLazy<ViewLogManagerFactory>, ILogManagerFactory<LogConfiguration>
    {
        private readonly ConcurrentDictionary<string, IManagedLogWriter> manage_writers_ = new();

        private readonly ConcurrentDictionary<string, IConfigurationManager<LogConfiguration>> configuration_manager_ =
            new();

        private bool is_disposed_ = false;

#if UNITY_EDITOR
        private static bool editor_events_registered_ = false;
        private static readonly object register_lock_ = new();
#endif

        protected override void InitializationInternal()
        {
            // 添加一个默认的数据库作为日志中心
            var configuration = new NetJsonLogConfigurationManager(new LogConfiguration
            {
                BaseLogFileName = "unity_logs",
                LogExtensionName = "db",
                EnableRetention = true,
                RetentionDays = 1
            });

            var serilog_writer = new SerilogWriter(configuration);

            TryRegisterLogWriter(FixedValues.kDefaultLogSpace, serilog_writer, configuration);

#if UNITY_EDITOR
            lock (register_lock_)
            {
                if (!editor_events_registered_)
                {
                    EditorApplication.playModeStateChanged += OnPlayModeStateChangedEditor;
                    editor_events_registered_ = true;
                }
            }
#endif
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChangedEditor(PlayModeStateChange state)
        {
            if (state is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.ExitingPlayMode)) return;
            if (Instance is { is_disposed_: false } factory)
                try
                {
                    factory.EnsureAllWritersInitialized();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error during delayed writer initialization: {e.Message}");
                }
        }
#endif

        public void EnsureAllWritersInitialized()
        {
            if (is_disposed_) return;
            var writers = manage_writers_.Values.ToList();

            if (writers.Count == 0) return;

            foreach (var writer in writers)
                try
                {
                    writer?.EnsureInitialized();
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        $"[ViewLogManagerFactory] Error calling EnsureInitialized on writer {writer?.GetType().Name}: " +
                        $"{e.Message}\n{e.StackTrace}");
                }
        }


        /// <summary>
        /// Releases all unmanaged and managed resources associated with the current instance
        /// of the ViewLogManagerFactory class. This method is the public implementation
        /// of the IDisposable interface and ensures that resources are cleaned up properly.
        /// Once disposed, the instance should no longer be used.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources used by the ViewLogManagerFactory instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from the Dispose method (true) or from the finalizer (false).</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!is_disposed_)
            {
                if (disposing) ShutdownAll();

#if UNITY_EDITOR
                lock (register_lock_)
                {
                    if (editor_events_registered_)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChangedEditor;
                        editor_events_registered_ = false;
                    }
                }
#endif

                is_disposed_ = true;
            }
        }

        /// <summary>
        /// Attempts to retrieve a managed log writer by its name.
        /// </summary>
        /// <param name="name">The name of the log writer to retrieve.</param>
        /// <returns>The managed log writer associated with the specified name, or null if no writer is found.</returns>
        public IManagedLogWriter TryGetLogWriter(string name)
        {
            return manage_writers_.GetValueOrDefault(name);
        }

        public bool RegisterLogWriter(string name, IManagedLogWriter writer)
        {
            var success = TryRegisterLogInternal(name, writer, null);

            if (success) writer?.EnsureInitialized();

            return success;
        }

        public bool TryUnregisterLogWriter(string name, out IManagedLogWriter writer)
        {
            if (string.IsNullOrEmpty(name))
            {
                writer = null;
                return false;
            }

            var success = manage_writers_.TryRemove(name, out writer);
            if (configuration_manager_.TryRemove(name, out var configuration)) Debug.Log("成功移除configuration");
            if (success)
            {
                Debug.Log($"LogManagerFactory: Unregistered log writer '{name}'.");
                writer?.Dispose();
            }

            return success;
        }

        public IEnumerable<IManagedLogWriter> GetAllWriters()
        {
            return manage_writers_.Values.ToList();
        }

        public void ShutdownAll()
        {
            if (is_disposed_) return;

            // 创建快照
            var writers_to_shutdown = manage_writers_.Values.ToList();
            var configs_to_save = configuration_manager_.Values.ToList();

            manage_writers_.Clear();
            configuration_manager_.Clear();

            foreach (var config in configs_to_save)
                try
                {
                    config?.Save(config.ManagerConfiguration, config.GetDefaultPath());
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ViewLogManagerFactory] Error saving configuration: {e.Message}\n{e.StackTrace}");
                }

            foreach (var writer in writers_to_shutdown)
                try
                {
                    writer?.Dispose();
                }
                catch (Exception e)
                {
                    if (writer != null)
                        Debug.LogError(
                            $"LogManagerFactory: Error shutting down log writer {writer.GetType().Name}: {e.Message}\n{e.StackTrace}");
                }
        }

        public IConfigurationManager<LogConfiguration> GetConfigurationManager(string name)
        {
            return configuration_manager_.GetValueOrDefault(name);
        }

        public bool TryRegisterLogWriter(string name, IManagedLogWriter writer,
            IConfigurationManager<LogConfiguration> manager)
        {
            var success = TryRegisterLogInternal(name, writer, manager);

            if (success) writer?.EnsureInitialized();

            return success;
        }

        private bool TryRegisterLogInternal(string name, IManagedLogWriter writer,
            IConfigurationManager<LogConfiguration> manager)
        {
            if (string.IsNullOrEmpty(name) || writer == null || manager == null)
            {
                Debug.LogError("[ViewLogManagerFactory] Cannot register log writer: name, writer, or manager is null.");
                return false;
            }

            if (!manage_writers_.TryAdd(name, writer))
            {
                Debug.LogError($"[ViewLogManagerFactory] Failed to add log writer '{name}' to dictionary.");
                return false;
            }

            if (!configuration_manager_.TryAdd(name, manager))
            {
                Debug.LogError(
                    $"[ViewLogManagerFactory] Registered log writer '{name}', but failed to register configuration manager. Unregistering writer.");
                manage_writers_.TryRemove(name, out _); // 回滚 writer 的注册
                return false;
            }

            return true;
        }

        public IEnumerable<IConfigurationManager<LogConfiguration>> GetAllConfigurationManagers()
        {
            return configuration_manager_.Values.ToList();
        }
    }
}