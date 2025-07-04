using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExTools;
using Script.Tool;

namespace Script.Save.Serialization
{
    /// <summary>
    /// Defines a contract for file storage operations, with methods to handle serialization and deserialization
    /// of data objects. Implementations of this interface provide capabilities for file management,
    /// including saving, loading, and registering file type-specific handlers.
    /// </summary>
    public interface IFileStorage
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
        void RegisterFileTypeHandler(IFileTypeHandler handler);
    }

    /// <summary>
    /// Defines methods to handle specific file types for serialization and deserialization.
    /// Implementations of this interface manage custom rules for saving and loading data to and from files
    /// with particular formats or extensions.
    /// </summary>
    public interface IFileTypeHandler
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
        bool SaveToFile<T>(IFileStorage serializer, T obj, string file_path);

        /// <summary>
        /// 自定义文件加载规则模板，需要配合SaveToFile使用。其可以根据你设定的规则来讲自定义文件规则反序列化。
        /// </summary>
        /// <param name="serializer">文件存储对象</param>
        /// <param name="file_path">文件路径</param>
        /// <param name="result">反序列化对象</param>
        /// <typeparam name="T">反序列化对象类型</typeparam>
        /// <returns>如果该文件可以按照你设定的规则反序列出来则返回true，否则false</returns>
        bool TryLoadFromFile<T>(IFileStorage serializer, string file_path, out T result);
    }

    /// <summary>
    /// Represents an asynchronous file type handler contract, inheriting from the base file type handler interface.
    /// This interface defines methods for asynchronously saving and loading objects to and from files.
    /// Implementations should handle type-specific serialization and deserialization operations in an asynchronous context,
    /// supporting optional progress tracking and cancellation tokens for enhanced flexibility.
    /// </summary>
    public interface IAsyncFileTypeHandler : IFileTypeHandler
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
        UniTask<bool> SaveToFileAsync<T>(IFileStorage serializer, T obj, string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to asynchronously load an object of type <typeparamref name="T"/> from a file.
        /// </summary>
        /// <typeparam name="T">The type of the object to be loaded.</typeparam>
        /// <param name="serializer">The file storage instance responsible for handling file operations.</param>
        /// <param name="filePath">The path to the file to load the object from.</param>
        /// <param name="cancellationToken">The token that can be used to cancel the loading operation.</param>
        /// <returns>A UniTask containing a tuple with a boolean indicating success or failure, and the deserialized object of type <typeparamref name="T"/> if successful.</returns>
        UniTask<(bool success, T result)> TryLoadFromFileAsync<T>(IFileStorage serializer, string filePath,
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
        UniTask<bool> SaveToFileAsync<T>(IFileStorage serializer, T obj, string filePath, IProgress<float> progress,
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
        UniTask<(bool success, T result)> TryLoadFromFileAsync<T>(IFileStorage serializer, string filePath,
            IProgress<float> progress, CancellationToken cancellationToken = default);
    }


    public abstract class FileStorageBase : IFileStorage
    {
        private readonly List<IFileTypeHandler> file_type_handlers_ = new();

        public abstract string[] GetSupportedFileExtensions();

        public virtual bool CanHandleFile(string file_path, bool read_content = false)
        {
            if (!File.Exists(file_path)) return false;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            var can_handle_extension = Array.Exists(GetSupportedFileExtensions(), ext =>
                (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase)
            );

            if (!can_handle_extension || !read_content) return can_handle_extension;

            // 如果需要读取内容判断，尝试读取文件头
            try
            {
                var header = new byte[Math.Min(1024, new FileInfo(file_path).Length)];

                using var fs = new FileStream(file_path, FileMode.Open, FileAccess.Read);
                fs.Read(header, 0, header.Length);

                // 尝试通过文件类型处理器判断
                foreach (var handler in file_type_handlers_)
                    if (handler.CanHandleContent(header))
                        return true;

                // 基本判断，例如文本文件以BOM开头
                return CanHandleFileContent(header);
            }
            catch
            {
                return false;
            }
        }

        public void RegisterFileTypeHandler(IFileTypeHandler handler)
        {
            if (!file_type_handlers_.Contains(handler)) file_type_handlers_.Add(handler);
        }

        protected virtual bool CanHandleFileContent(byte[] header)
        {
            // 默认不基于判断，由子类覆盖实现特定逻辑
            return false;
        }

        protected IEnumerable<IFileTypeHandler> GetFileTypeHandlers()
        {
            return file_type_handlers_;
        }

        public abstract void SaveToFile<T>(T obj, string file_path);

        public abstract T LoadFromFile<T>(string file_path);

        public abstract UniTask SaveToFileAsync<T>(T obj, string file_path,
            CancellationToken cancellation_token = default);

        public abstract UniTask<T> LoadFromFileAsync<T>(string file_path,
            CancellationToken cancellation_token = default);

        public abstract UniTask SaveToFileAsync<T>(T obj, string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default);

        public abstract UniTask<T> LoadFromFileAsync<T>(string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default);
    }

    public abstract class TextFileStorage : FileStorageBase
    {
        protected override bool CanHandleFileContent(byte[] header)
        {
            return FileExTool.IsTextFile(header);
        }

        /// <summary>
        /// 将对象序列化为文本
        /// </summary>
        /// <param name="obj">需要转换为文本的对象</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象序列化后的文本</returns>
        protected abstract string SerializeToText<T>(T obj);

        protected abstract T DeserializeFromText<T>(string text);

        public override void SaveToFile<T>(T obj, string file_path)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(file_path);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                if (directory != null)
                    Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文本处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        ext => (ext.StartsWith(".") ? ext : "." + ext).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.SaveToFile(this, obj, file_path))
                        return;

            // 默认文本保存
            var content = SerializeToText(obj);
            File.WriteAllText(file_path, content);
        }

        public override T LoadFromFile<T>(string file_path)
        {
            if (!File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文件类型处理
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.TryLoadFromFile(this, file_path, out T result))
                        return result;

            // 默认文本加载
            var content = File.ReadAllText(file_path);
            return DeserializeFromText<T>(content);
        }

        public override async UniTask SaveToFileAsync<T>(T obj, string file_path, CancellationToken cancellation_token = default)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(file_path);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory)) 
                Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            
            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
            {
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    // 如果处理器支持异步，使用异步方法
                    if (handler is IAsyncFileTypeHandler async_handler)
                    {
                        if (await async_handler.SaveToFileAsync(this,obj,file_path,cancellation_token))
                        {
                            return;
                        }
                    }
                    else if (handler.SaveToFile(this,obj,file_path))
                    {
                        return;
                    }
                }
            }
            
            // 默认文件保存 - 使用UniTask读写文件
            var content = SerializeToText(obj);
            await UniTask.RunOnThreadPool(() =>
            {
                File.WriteAllText(file_path, content);
            }, cancellationToken: cancellation_token);
        }
    }

    public abstract class BinaryFileStorage : FileStorageBase
    {
        protected override bool CanHandleFileContent(byte[] header)
        {
            return !FileExTool.IsTextFile(header);
        }

        protected abstract byte[] SerializeToBinary<T>(T obj);

        protected abstract T DeserializeFromBinary<T>(byte[] data);

        public override void SaveToFile<T>(T obj, string file_path)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(file_path);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        ext => (ext.StartsWith(".") ? ext : "," + ext).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.SaveToFile(this, obj, file_path))
                        return;

            // 默认二进制保存
            var content = SerializeToBinary(obj);
            File.WriteAllBytes(file_path, content);
        }

        public override T LoadFromFile<T>(string file_path)
        {
            if (!File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.TryLoadFromFile(this, file_path, out T result))
                        return result;

            // 默认二进制加载
            var content = File.ReadAllBytes(file_path);
            return DeserializeFromBinary<T>(content);
        }
    }
}