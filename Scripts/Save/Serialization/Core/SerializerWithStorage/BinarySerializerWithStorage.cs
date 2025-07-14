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
    /// Provides an abstract base class for binary serializers with integrated file storage capabilities,
    /// enabling serialization and deserialization to and from binary formats, as well as handling file operations.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used for transforming data during serialization.</typeparam>
    /// <typeparam name="TOptions">The type of the configuration options used to customize the serializer behavior.</typeparam>
    public abstract class
        BinarySerializerWithStorage<TConverter, TOptions> : AsyncSerializerBase<TConverter, TOptions>,
        IBinarySerializer, ISerializerFileStorage
    {
        protected BinarySerializerWithStorage(SerializationSettings shared_settings = null) : base(shared_settings)
        {
        }

        public override Type GetSerializedDataType()
        {
            return typeof(byte[]);
        }

        /// <summary>
        /// 序列化对象为通用对象
        /// </summary>
        public override object Serialize<T>(T obj)
        {
            return SerializeToBinary(obj);
        }

        /// <summary>
        /// 从通用对象反序列化
        /// </summary>
        public override T Deserialize<T>(object data)
        {
            if (data is byte[] binary) return DeserializeFromBinary<T>(binary);

            throw new ArgumentException($"Expected byte[] data, got {data?.GetType().Name ?? "null"}");
        }

        /// <summary>
        /// 序列化为二进制
        /// </summary>
        public abstract byte[] SerializeToBinary<T>(T obj);

        /// <summary>
        /// 从二进制反序列化
        /// </summary>
        public abstract T DeserializeFromBinary<T>(byte[] data);

        #region FileStorage成员

        private readonly List<ISerializerFileTypeHandler> file_type_handlers_ = new();
        private ISerializerFileStorage _serializerFileStorageImplementation;

        /// <summary>
        /// 获取支持的文件扩展名
        /// </summary>
        public abstract string[] GetSupportedFileExtensions();

        /// <summary>
        /// Determines if the serializer can handle the specified file based on its path and optional content validation.
        /// </summary>
        /// <param name="filePath">The path to the file to be checked.</param>
        /// <param name="readContent">Indicates whether to read and validate the file's content to ensure it is supported.</param>
        /// <returns>True if the file is supported by the serializer, otherwise false.</returns>
        public virtual bool CanHandleFile(string filePath, bool readContent = false)
        {
            if (!File.Exists(filePath)) return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var can_handle_extension = Array.Exists(GetSupportedFileExtensions(), ext =>
                (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (!can_handle_extension || !readContent) return can_handle_extension;

            // 如果需要读取内容判断，尝试读取文件头
            try
            {
                var header = new byte[Math.Min(1024, new FileInfo(filePath).Length)];
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var read = fs.Read(header, 0, header.Length);

                if (read > 0)
                {
                    if (read < header.Length) Array.Resize(ref header, read);

                    // 尝试通过文件类型处理器判断
                    foreach (var handler in file_type_handlers_)
                        if (handler.CanHandleContent(header))
                            return true;

                    // 基本判断
                    return CanHandleFileContent(header);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether the file content provided by the header can be handled by the serializer.
        /// </summary>
        /// <param name="fileHeader">The array of bytes representing the header of the file to be checked.</param>
        /// <returns>A boolean value indicating whether the file content can be handled (true) or not (false).</returns>
        protected virtual bool CanHandleFileContent(byte[] fileHeader)
        {
            // 可以检查二进制文件的特定标记
            // 例如，许多二进制格式有特定的魔数作为开头

            // 默认情况下，如果不是文本文件，就假设是二进制文件
            return !FileExTool.IsTextFile(fileHeader);
        }

        /// <summary>
        /// 注册文件类型处理器
        /// </summary>
        public void RegisterFileTypeHandler(ISerializerFileTypeHandler handler)
        {
            if (!file_type_handlers_.Contains(handler)) file_type_handlers_.Add(handler);
        }

        /// <summary>
        /// 获取所有文件类型处理器
        /// </summary>
        protected IEnumerable<ISerializerFileTypeHandler> GetFileTypeHandlers()
        {
            return file_type_handlers_;
        }

        /// <summary>
        /// 保存对象到文件
        /// </summary>
        public virtual void SaveToFile<T>(T obj, string filePath)
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(), ext =>
                        (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase)))
                    if (handler.SaveToFile(this, obj, filePath))
                        return;

            // 默认二进制保存
            var content = SerializeToBinary(obj);
            File.WriteAllBytes(filePath, content);
        }

        /// <summary>
        /// 从文件加载对象
        /// </summary>
        public virtual T LoadFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath)) return default;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(), ext =>
                        (ext.StartsWith(".") ? ext : "." + ext).Equals(extension, StringComparison.OrdinalIgnoreCase)))
                    if (handler.TryLoadFromFile(this, filePath, out T result))
                        return result;

            // 默认二进制加载
            var content = File.ReadAllBytes(filePath);
            return DeserializeFromBinary<T>(content);
        }


        public virtual async UniTask SaveToFileAsync<T>(T obj, string file_path,
            CancellationToken cancellation_token = default)
        {
            await SaveToFileAsyncTemp(obj, file_path, cancellation_token: cancellation_token);
        }

        public virtual async UniTask<T> LoadFromFileAsync<T>(string file_path,
            CancellationToken cancellation_token = default)
        {
            return await LoadFromFileAsyncTemp<T>(file_path, cancellation_token: cancellation_token);
        }

        public virtual async UniTask SaveToFileAsync<T>(T obj, string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default)
        {
            await SaveToFileAsyncTemp(obj, file_path, progress, cancellation_token);
        }

        public virtual UniTask<T> LoadFromFileAsync<T>(string file_path, IProgress<float> progress,
            CancellationToken cancellation_token = default)
        {
            return LoadFromFileAsyncTemp<T>(file_path, progress, cancellation_token);
        }

        private async UniTask SaveToFileAsyncTemp<T>(T obj, string file_path, IProgress<float> progress = null,
            CancellationToken cancellation_token = default)
        {
            progress?.Report(0f);

            // 确保目录存在
            var directory = Path.GetDirectoryName(file_path);
            if (!Directory.Exists(directory) && !string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            progress?.Report(0.5f);

            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        ext => (ext.StartsWith(".") ? ext : "." + ext).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
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

            // 默认二进制保存
            var content = SerializeToBinary(obj);
            await UniTask.RunOnThreadPool(() => File.WriteAllBytes(file_path, content),
                cancellationToken: cancellation_token);
            progress?.Report(1.0f);
        }

        private async UniTask<T> LoadFromFileAsyncTemp<T>(string file_path, IProgress<float> progress = null,
            CancellationToken cancellation_token = default)
        {
            progress?.Report(0f);

            if (File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();
            progress?.Report(0.5f);

            // 尝试使用文件处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        ext => (ext.StartsWith(".") ? ext : "." + ext).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                {
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

            var content = await UniTask.RunOnThreadPool(() => File.ReadAllBytes(file_path),
                cancellationToken: cancellation_token);

            progress?.Report(1.0f);
            return DeserializeFromBinary<T>(content);
        }

        #endregion
    }
}
