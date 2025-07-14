using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExTools;
using Save.Serialization.Core.FileStorage;
using Save.Serialization.Core.TypeConverter;
using Script.Save.Serialization;

namespace Save.Serialization.Core.SerializerWithStorage
{
    /// <summary>
    /// Represents an abstract base class for text-based serializers that integrate file storage capabilities.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used to handle type transformations during serialization and deserialization.</typeparam>
    /// <typeparam name="TOptions">The type of options used to configure the serializer and its behavior.</typeparam>
    public abstract class
        TextSerializerWithStorage<TConverter, TOptions> : AsyncSerializerBase<TConverter, TOptions>,
        ITextSerializer, ISerializerFileStorage
    {
        protected TextSerializerWithStorage(SerializationSettings shared_settings = null) : base(shared_settings)
        {
        }

        public override Type GetSerializedDataType()
        {
            return typeof(string);
        }

        public override object Serialize<T>(T obj)
        {
            return SerializeToText(obj);
        }

        public override T Deserialize<T>(object data)
        {
            if (data is string text) return DeserializeFromText<T>(text);

            throw new ArgumentException($"Expected string data, got {data?.GetType().Name ?? "null"}");
        }

        public abstract string SerializeToText<T>(T obj);

        public abstract T DeserializeFromText<T>(string text);

        #region FileStorage成员

        private readonly List<ISerializerFileTypeHandler> file_type_handlers_ = new();

        public abstract string[] GetSupportedFileExtensions();

        public virtual bool CanHandleFile(string file_path, bool read_content = false)
        {
            if (!File.Exists(file_path)) return false;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            var can_handle_extension = Array.Exists(GetSupportedFileExtensions(),
                ext => (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (!can_handle_extension || !read_content) return can_handle_extension;

            // 如果需要读取内容判断，尝试读取文件头
            try
            {
                var header = new byte[Math.Min(1024, new FileInfo(file_path).Length)];
                using var fs = new FileStream(file_path, FileMode.Open, FileAccess.Read);
                var read = fs.Read(header, 0, header.Length);

                // 只有在确实读取到数据时才进行后续处理
                if (read > 0)
                {
                    // 如果读取的数据少于请求的数据，可以调整header数据
                    Array.Resize(ref header, read);

                    // 尝试通过文件类型处理器判断
                    foreach (var handler in file_type_handlers_)
                        if (handler.CanHandleContent(header))
                            return true;

                    // 基本判断，例如文本以BOM开头等
                    return CanHandleFileContent(header);
                }

                // 文件为空或读取失败
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the given file header indicates that the file content can be handled.
        /// </summary>
        /// <param name="fileHeader">The header bytes of the file to analyze.</param>
        /// <returns>True if the file content can be handled; otherwise, false.</returns>
        protected virtual bool CanHandleFileContent(byte[] fileHeader)
        {
            return FileExTool.IsTextFile(fileHeader);
        }

        public void RegisterFileTypeHandler(ISerializerFileTypeHandler handler)
        {
            if (!file_type_handlers_.Contains(handler)) file_type_handlers_.Add(handler);
        }

        /// <summary>
        /// Retrieves the collection of file type handlers associated with the serializer.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="ISerializerFileTypeHandler"/> objects that can handle
        /// specific file types.</returns>
        protected IEnumerable<ISerializerFileTypeHandler> GetFileTypeHandlers()
        {
            return file_type_handlers_;
        }

        public virtual void SaveToFile<T>(T obj, string file_path)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(file_path);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文本类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.SaveToFile(this, obj, file_path))
                        return;

            // 默认文本保存
            var content = SerializeToText(obj);
            File.WriteAllText(file_path, content);
        }

        public T LoadFromFile<T>(string file_path)
        {
            if (!File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文本类型处理数据
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(), ext =>
                        (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase)))
                    if (handler.TryLoadFromFile(this, file_path, out T result))
                        return result;

            // 使用默认文本加载
            var content = File.ReadAllText(file_path);
            return DeserializeFromText<T>(content);
        }

        public virtual async UniTask SaveToFileAsync<T>(T obj, string file_path,
            CancellationToken cancellation_token = default)
        {
            await SaveToFileAsyncTemp(this, file_path, cancellation_token: cancellation_token);
        }

        public virtual async UniTask<T> LoadFromFileAsync<T>(string file_path,
            CancellationToken cancellation_token = default)
        {
            return await LoadFromFileAsyncTemp<T>(file_path, cancellation_token: cancellation_token);
        }

        public virtual async UniTask SaveToFileAsync<T>(T obj, string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default)
        {
            await SaveToFileAsyncTemp(this, file_path, progress, cancellation_token);
        }

        public virtual async UniTask<T> LoadFromFileAsync<T>(string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default)
        {
            return await LoadFromFileAsyncTemp<T>(file_path, progress, cancellation_token);
        }

        private async UniTask SaveToFileAsyncTemp<T>(T obj, string file_path, IProgress<float> progress = null,
            CancellationToken cancellation_token = default)
        {
            // 报告初始进度
            progress?.Report(0f);

            var directory = Path.GetDirectoryName(file_path);
            if (!Directory.Exists(directory) && !string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            progress?.Report(0.5f);

            // 尝试使用文本类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    // 如果处理器支持异步，使用异步方法
                    if (handler is IAsyncSerializerFileTypeHandler async_handler)
                    {
                        if (await async_handler.SaveToFileAsync(this, obj, file_path, progress, cancellation_token))
                        {
                            progress?.Report(1.0f);

                            return;
                        }
                        else if (handler.SaveToFile(this, obj, file_path))
                        {
                            progress?.Report(1.0f);
                            return;
                        }
                    }

            // 默认文本保存 - 使用UniTask读写文件
            var content = SerializeToText(obj);
            await UniTask.RunOnThreadPool(() => { File.WriteAllText(file_path, content); },
                cancellationToken: cancellation_token);
            progress?.Report(1.0f);
        }

        private async UniTask<T> LoadFromFileAsyncTemp<T>(string file_path, IProgress<float> progress = null,
            CancellationToken cancellation_token = default)
        {
            // 报告初始进度
            progress?.Report(0f);

            if (!File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            progress?.Report(0.2f);

            // 尝试使用文本类型处理数据
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(), ext =>
                        (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase)))
                {
                    progress?.Report(0.8f);
                    // 如果处理器支持异步，使用异步方法
                    if (handler is IAsyncSerializerFileTypeHandler async_handler)
                    {
                        var result =
                            await async_handler.TryLoadFromFileAsync<T>(this, file_path, progress, cancellation_token);
                        if (result.success)
                        {
                            progress?.Report(1.0f);
                            return result.result;
                        }
                    }
                    else if (handler.TryLoadFromFile(this, file_path, out T result))
                    {
                        progress?.Report(1.0f);
                        return result;
                    }
                }

            // 默认文本加载 - 使用UniTask读写文件
            var content = await UniTask.RunOnThreadPool(() => File.ReadAllText(file_path),
                cancellationToken: cancellation_token);
            progress?.Report(1.0f);

            return DeserializeFromText<T>(content);
        }

        #endregion
    }
}