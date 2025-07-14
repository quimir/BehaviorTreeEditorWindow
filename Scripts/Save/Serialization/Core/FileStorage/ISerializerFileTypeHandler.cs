using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Save.Serialization.Core.FileStorage
{
    /// <summary>
    /// Defines methods to handle specific file types for serialization and deserialization.
    /// Implementations of this interface manage custom rules for saving and loading data to and from files
    /// with particular formats or extensions.
    /// </summary>
    public interface ISerializerFileTypeHandler
    {
        /// <summary>
        /// 获取所有可以处理的文件类型扩展名
        /// </summary>
        /// <returns>所有可以处理的文件类型扩展名</returns>
        string[] GetSupportedExtensions();

        /// <summary>
        /// 检查该数据类型是否可以进行处理
        /// </summary>
        /// <param name="file_header">文件头，一般为前1024个字符</param>
        /// <returns></returns>
        bool CanHandleContent(byte[] file_header);

        /// <summary>
        /// 自定义保存规则模板，假如你想让文件可以按照你设定的规则来保存具体的数据，那么就实现该接口，该接口可以保证文件可以按照你的规则来保存文件。
        /// </summary>
        /// <param name="serializer">文件存储对象</param>
        /// <param name="obj">序列化数据</param>
        /// <param name="file_path">文件路径</param>
        /// <typeparam name="T">序列化数据类型</typeparam>
        /// <returns>如果该对象可以按照你的规则存储则返回true，否则false</returns>
        bool SaveToFile<T>(global::Save.Serialization.Core.FileStorage.ISerializerFileStorage serializer, T obj, string file_path);

        /// <summary>
        /// 自定义文件加载规则模板，需要配合SaveToFile使用。其可以根据你设定的规则来讲自定义文件规则反序列化。
        /// </summary>
        /// <param name="serializer">文件存储对象</param>
        /// <param name="file_path">文件路径</param>
        /// <param name="result">反序列化对象</param>
        /// <typeparam name="T">反序列化对象类型</typeparam>
        /// <returns>如果该文件可以按照你设定的规则反序列出来则返回true，否则false</returns>
        bool TryLoadFromFile<T>(global::Save.Serialization.Core.FileStorage.ISerializerFileStorage serializer, string file_path, out T result);
    }

    /// <summary>
    /// Represents an asynchronous file type handler contract, inheriting from the base file type handler interface.
    /// This interface defines methods for asynchronously saving and loading objects to and from files.
    /// Implementations should handle type-specific serialization and deserialization operations in an asynchronous context,
    /// supporting optional progress tracking and cancellation tokens for enhanced flexibility.
    /// </summary>
    public interface IAsyncSerializerFileTypeHandler : ISerializerFileTypeHandler
    {
        /// <summary>
        /// Asynchronously saves an object to a specified file path using the provided serializer.
        /// </summary>
        /// <param name="serializer">The file storage mechanism used for saving the object.</param>
        /// <param name="obj">The object to be saved to the file.</param>
        /// <param name="filePath">The path of the file where the object will be saved.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests during the save operation.</param>
        /// <typeparam name="T">The type of the object being saved.</typeparam>
        /// <returns>A task that represents the asynchronous save operation, containing a boolean value indicating success or failure.</returns>
        UniTask<bool> SaveToFileAsync<T>(global::Save.Serialization.Core.FileStorage.ISerializerFileStorage serializer, T obj, string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to asynchronously load an object of type <typeparamref name="T"/> from a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to be loaded.</typeparam>
        /// <param name="serializer">The file storage instance responsible for handling file operations.</param>
        /// <param name="filePath">The path to the file to load the object from.</param>
        /// <param name="cancellationToken">The token that can be used to cancel the loading operation.</param>
        /// <returns>A UniTask containing a tuple with a boolean indicating success or failure, and the deserialized object of type <typeparamref name="T"/> if successful.</returns>
        UniTask<(bool success, T result)> TryLoadFromFileAsync<T>(global::Save.Serialization.Core.FileStorage.ISerializerFileStorage serializer, string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously saves the specified object to a file with progress reporting and cancellation support.
        /// </summary>
        /// <typeparam name="T">The type of the object to be saved.</typeparam>
        /// <param name="serializer">The serializer instance used for file operations.</param>
        /// <param name="obj">The object to be saved to the file.</param>
        /// <param name="filePath">The path to the file where the object will be saved.</param>
        /// <param name="progress">An object for reporting progress of the save operation.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>Returns true if the object was successfully saved, otherwise false.</returns>
        UniTask<bool> SaveToFileAsync<T>(global::Save.Serialization.Core.FileStorage.ISerializerFileStorage serializer, T obj, string filePath, IProgress<float> progress,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to load data of the specified type from the given file path using the provided serializer.
        /// </summary>
        /// <typeparam name="T">The type of the data to be deserialized.</typeparam>
        /// <param name="serializer">An instance of the file storage serializer to handle the deserialization process.</param>
        /// <param name="filePath">The path to the file that contains the data to be loaded.</param>
        /// <param name="progress">A progress reporting object to track the operation's progress.</param>
        /// <param name="cancellationToken">Token to observe the operation for cancellation.</param>
        /// <returns>A task representing the asynchronous operation, containing a tuple indicating whether the load was successful and the deserialized result.</returns>
        UniTask<(bool success, T result)> TryLoadFromFileAsync<T>(global::Save.Serialization.Core.FileStorage.ISerializerFileStorage serializer, string filePath,
            IProgress<float> progress, CancellationToken cancellationToken = default);
    }
}