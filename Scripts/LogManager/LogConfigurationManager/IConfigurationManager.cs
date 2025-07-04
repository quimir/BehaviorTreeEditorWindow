namespace LogManager.LogConfigurationManager
{
    /// <summary>
    /// Represents a generic interface for managing configuration settings.
    /// Provides methods for loading, saving, and retrieving configuration objects.
    /// </summary>
    /// <typeparam name="T">
    /// The type of configuration object managed by the implementation.
    /// Must be a class with a parameterless constructor.
    /// </typeparam>
    public interface IConfigurationManager<T> where T : class, new()
    {
        /// <summary>
        /// Loads a configuration from a specified file path.
        /// If the file does not exist and the <paramref name="create_default_missing"/> parameter is set to true,
        /// a default configuration is created, saved to the specified file path, and then returned.
        /// </summary>
        /// <param name="file_path">The path to the configuration file. If null, the default path is used.</param>
        /// <param name="create_default_missing">
        /// A boolean flag indicating whether to create and save a default configuration
        /// if the file does not exist. If set to false and the file is missing, an exception is thrown.
        /// </param>
        /// <returns>The loaded configuration object.</returns>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown when the file does not exist and <paramref name="create_default_missing"/> is false.
        /// </exception>
        T Load(string file_path, bool create_default_missing = true);

        /// <summary>
        /// Saves the specified configuration object to the provided file path.
        /// If the file path is null, the configuration is saved to the default path.
        /// </summary>
        /// <param name="configuration">The configuration object to be saved. Must not be null.</param>
        /// <param name="file_path">The path where the configuration should be saved. If null, the default path is used.</param>
        /// <returns>True if the configuration was successfully saved; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when the <paramref name="configuration"/> is null.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown when an I/O error occurs during the save operation.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// Thrown when the application does not have permission to write to the specified file path.
        /// </exception>
        bool Save(T configuration, string file_path);

        /// <summary>
        /// Represents configuration settings for managing log-related operations.
        /// This property is used for retrieving or setting the current configuration object.
        /// </summary>
        T ManagerConfiguration { get; }

        /// <summary>
        /// Retrieves the default file path for the configuration.
        /// This is typically used when no specific path is provided for a configuration file.
        /// </summary>
        /// <returns>The default file path as a string.</returns>
        string GetDefaultPath();
    }
}
