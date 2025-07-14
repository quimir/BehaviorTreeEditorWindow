using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Save.Serialization.Core.FileStorage
{
    /// <summary>
    /// Defines methods to handle file storage operations for serialized data.
    /// </summary>
    public interface ISerializerFileStorage
    {
        /// <summary>
        /// 获取所有可以处理的文件类型扩展名
        /// </summary>
        /// <returns>所有可以处理的文件类型扩展名</returns>
        string[] GetSupportedFileExtensions();
        
        /// <summary>
        /// 检查是否可以处理该文件
        /// </summary>
        /// <param name="file_path">文件路径</param>
        /// <param name="read_content">是否要根据内容来判断</param>
        /// <returns>如果可以处理该文件则返回true，否则false</returns>
        bool CanHandleFile(string file_path, bool read_content = false);

        /// <summary>
        /// 将数据序列化之后保存到文件当中。
        /// </summary>
        /// <param name="obj">数据对象</param>
        /// <param name="file_path">文件路径</param>
        /// <typeparam name="T">数据对象类型</typeparam>
        void SaveToFile<T>(T obj, string file_path);
        
        /// <summary>
        /// 从文件当中加载数据并将其反序列。
        /// </summary>
        /// <param name="file_path">文件路径</param>
        /// <typeparam name="T">反序列化类型</typeparam>
        /// <returns>反序列化数据</returns>
        T LoadFromFile<T>(string file_path);
        
        /// <summary>
        /// 使用异步方法将数据序列化之后保存在文件当中(大文件时使用最佳)
        /// </summary>
        /// <param name="obj">数据对象</param>
        /// <param name="file_path">文件路径</param>
        /// <param name="cancellation_token">取消异步指令</param>
        /// <typeparam name="T">数据对象类型</typeparam>
        /// <returns></returns>
        UniTask SaveToFileAsync<T>(T obj, string file_path, CancellationToken cancellation_token = default);

        /// <summary>
        /// Asynchronously loads data from a specified file and deserializes it into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="file_path">The path of the file to load the data from.</param>
        /// <param name="cancellation_token">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized object of type <typeparamref name="T"/>.</returns>
        UniTask<T> LoadFromFileAsync<T>(string file_path, CancellationToken cancellation_token = default);

        /// <summary>
        /// Asynchronously saves the specified object to a file with progress reporting and cancellation support.
        /// </summary>
        /// <typeparam name="T">The type of the object to be saved.</typeparam>
        /// <param name="obj">The object to save to the file.</param>
        /// <param name="file_path">The path to the file where the object will be saved.</param>
        /// <param name="progress">An optional progress reporter to report the save progress.</param>
        /// <param name="cancellation_token">An optional cancellation token to cancel the save operation.</param>
        /// <returns>A UniTask representing the asynchronous save operation.</returns>
        UniTask SaveToFileAsync<T>(T obj, string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default);

        /// <summary>
        /// Asynchronously loads and deserializes an object of the specified type from a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to be deserialized.</typeparam>
        /// <param name="file_path">The path to the file from which the object will be loaded.</param>
        /// <param name="progress">Reports the progress of the loading operation.</param>
        /// <param name="cancellation_token">A token to observe while waiting for the operation to complete, allowing it to be canceled.</param>
        /// <returns>A task that represents the asynchronous operation. The result contains the deserialized object of type T.</returns>
        UniTask<T> LoadFromFileAsync<T>(string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default);

        /// <summary>
        /// 注册文件类型处理器,此接口可以注册更多的文件类型并将其进行处理.
        /// </summary>
        /// <param name="handler">文件类型处理器对象</param>
        void RegisterFileTypeHandler(ISerializerFileTypeHandler handler);
    }
}
