using System;
using System.Collections.Generic;
using LogManager.Core;
using LogManager.LogConfigurationManager;

namespace LogManager.LogManagerFactory
{
    /// <summary>
    /// Represents a factory interface for creating and managing log writers.
    /// This interface provides methods to register, retrieve, and manage log writers,
    /// ensuring controlled logging operations within the application.
    /// </summary>
    public interface ILogManagerFactory : IDisposable
    {
        /// <summary>
        /// Attempts to retrieve a managed log writer by its name.
        /// </summary>
        /// <param name="name">The name of the log writer to retrieve.</param>
        /// <returns>The managed log writer associated with the specified name, or null if no writer is found.</returns>
        IManagedLogWriter TryGetLogWriter(string name);

        /// <summary>
        /// Registers a new log writer to the log manager with the specified name.
        /// </summary>
        /// <param name="name">The name to associate with the log writer.</param>
        /// <param name="writer">The log writer instance to register.</param>
        /// <returns>True if the log writer was successfully registered; otherwise, false.</returns>
        bool RegisterLogWriter(string name, IManagedLogWriter writer);

        /// <summary>
        /// Attempts to unregister a managed log writer by its name.
        /// </summary>
        /// <param name="name">The name of the log writer to unregister.</param>
        /// <param name="writer">The output parameter that will contain the unregistered log writer if the operation succeeds; otherwise, null.</param>
        /// <returns>True if the log writer was successfully unregistered; otherwise, false.</returns>
        bool TryUnregisterLogWriter(string name, out IManagedLogWriter writer);

        /// <summary>
        /// Retrieves all registered log writers managed by the log manager.
        /// </summary>
        /// <returns>An enumerable collection of all currently registered log writers.</returns>
        IEnumerable<IManagedLogWriter> GetAllWriters();

        /// <summary>
        /// Shuts down and cleans up all registered log writers managed by the log manager.
        /// </summary>
        void ShutdownAll();
    }

    /// <summary>
    /// Defines a factory interface for creating and managing log writers within an application.
    /// Provides methods for retrieving, registering, and managing log writers in accordance with the logging requirements.
    /// </summary>
    public interface ILogManagerFactory<T> : ILogManagerFactory where T : class, new()
    {
        /// <summary>
        /// Retrieves the configuration manager associated with the specified name.
        /// </summary>
        /// <param name="name">The name of the configuration manager to retrieve.</param>
        /// <returns>The configuration manager associated with the specified name, or null if no manager is found.</returns>
        IConfigurationManager<T> GetConfigurationManager(string name);

        /// <summary>
        /// Attempts to register a managed log writer with the logging system.
        /// </summary>
        /// <param name="name">The name of the log writer to register.</param>
        /// <param name="writer">The managed log writer instance to associate with the provided name.</param>
        /// <param name="manager">The configuration manager instance associated with the log writer.</param>
        /// <returns>True if the log writer and configuration manager were successfully registered; otherwise, false.</returns>
        bool TryRegisterLogWriter(string name, IManagedLogWriter writer, IConfigurationManager<T> manager);
        
        /// <summary>
        /// Retrieves all configuration managers associated with the logging system.
        /// </summary>
        /// <returns>A collection of configuration managers currently registered.</returns>
        IEnumerable<IConfigurationManager<T>> GetAllConfigurationManagers();
    }
}