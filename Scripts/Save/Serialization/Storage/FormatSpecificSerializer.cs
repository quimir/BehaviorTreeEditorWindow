using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExTools;
using Script.LogManager;
using Script.Save.Serialization;
using Script.Tool;

namespace Save.Serialization.Storage
{
    /// <summary>
    /// Represents an abstract base class for format-specific serializers that handle serialization and deserialization.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used for type transformations during serialization.</typeparam>
    /// <typeparam name="TOptions">The type of options used to configure the serializer.</typeparam>
    public abstract class FormatSpecificSerializer<TConverter, TOptions> : SerializerBase
    {
        /// <summary>
        /// 序列化选项
        /// </summary>
        protected TOptions Options;

        protected ISerializationContext Context;

        /// <summary>
        /// Manages the collection of type converters and provides functionality
        /// to retrieve and adapt converters for serialization or deserialization processes.
        /// </summary>
        protected readonly BaseTypeConverterManager<TConverter> ConvertersManager;

        /// <summary>
        /// A protected variable that holds shared serialization settings.
        /// These settings are utilized across different serializer implementations
        /// to ensure consistent behavior and configuration during serialization processes.
        /// </summary>
        protected SerializationSettings shared_settings_;

        protected FormatSpecificSerializer(SerializationSettings shared_settings = null)
        {
            shared_settings_ = shared_settings ?? new SerializationSettings
            {
                PreserveReferences = true
            };

            InitializeContextInternal();

            ConvertersManager = new BaseTypeConverterManager<TConverter>(CreateSpecificAdapter);

            ConvertersManager.OnRegistryChanged += OnConvertersManagedChanged;
        }

        /// <summary>
        /// Creates a format-specific adapter for serialization and deserialization processes.
        /// </summary>
        /// <param name="typeConverter">The type converter responsible for converting objects during serialization.</param>
        /// <param name="supportedType">The type that the adapter supports for serialization and deserialization.</param>
        /// <param name="context">The serialization context providing additional information and configuration.</param>
        /// <returns>A format-specific adapter used for handling serialization and deserialization of the specified type.</returns>
        protected abstract TConverter CreateSpecificAdapter(ITypeConverter typeConverter, Type supportedType,
            ISerializationContext context);

        /// <summary>
        /// Handles changes to the converter registry managed by the serializer.
        /// </summary>
        /// <remarks>
        /// This method is invoked whenever the converter registry is updated.
        /// It triggers a rebuild of the library-specific options to reflect the updated converters.
        /// </remarks>
        private void OnConvertersManagedChanged()
        {
            // When converters change, rebuild the library-specific options
            RebuildOptions();
        }

        /// <summary>
        /// Initializes the serialization context with format-specific configurations and settings specific to the derived serializer.
        /// This method is invoked during the serializer setup process and is expected to define all necessary initialization logic
        /// required for the serializer's functioning, including context configuration, dependency registration, or preconditions validation.
        /// </summary>
        protected abstract void InitializeContextInternal();

        public override ITypeConverterMessage GetTypeConverterManager()
        {
            return ConvertersManager;
        }

        protected override bool ValidateConverterType(object converter)
        {
            return converter is TConverter or ITypeConverter;
        }

        /// <summary>
        /// Applies the shared serialization settings to the format-specific options used by the serializer.
        /// </summary>
        protected abstract void ApplySharedSettingsToOptions();

        public virtual void UpdateSettings(SerializationSettings settings)
        {
            shared_settings_ = settings ?? new SerializationSettings();
            Context?.UpdateSettings(shared_settings_); // Update context if it holds settings

            // Applying settings might change how options are built, so rebuild.
            RebuildOptions();
        }

        /// <summary>
        /// Rebuilds the configuration options for the serializer specific to its format and type.
        /// </summary>
        /// <remarks>
        /// This method is responsible for reconstructing serializer options to align with updated settings
        /// or context changes. Implementations must ensure that the options reflect the current state
        /// of shared settings, context, and other serializer-specific requirements.
        /// </remarks>
        protected abstract void RebuildOptions();
    }

    /// <summary>
    /// Provides an abstract base class for asynchronous serializers, supporting serialization and deserialization operations that can run asynchronously.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used for processing objects during serialization.</typeparam>
    /// <typeparam name="TOptions">The type of configuration options utilized by the serializer.</typeparam>
    public abstract class AsyncSerializerBase<TConverter, TOptions> : FormatSpecificSerializer<TConverter, TOptions>,
        IAsyncSerializer
    {
        protected AsyncSerializerBase(SerializationSettings shared_settings = null) : base(shared_settings)
        {
        }

        public virtual UniTask<object> SerializeAsync<T>(T obj, CancellationToken cancellation_token = default)
        {
            return UniTask.RunOnThreadPool(() => Serialize(obj), cancellationToken: cancellation_token);
        }

        public virtual UniTask<T> DeserializeAsync<T>(object data, CancellationToken cancellation_token = default)
        {
            return UniTask.RunOnThreadPool(() => DeserializeAsync<T>(data), cancellationToken: cancellation_token);
        }
    }

    /// <summary>
    /// Represents an abstract base class for text-based serializers that integrate file storage capabilities.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used to handle type transformations during serialization and deserialization.</typeparam>
    /// <typeparam name="TOptions">The type of options used to configure the serializer and its behavior.</typeparam>
    public abstract class
        TextSerializerWithStorage<TConverter, TOptions> : AsyncSerializerBase<TConverter, TOptions>,
        ITextSerializer, IFileStorage
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

        private readonly List<IFileTypeHandler> file_type_handlers_ = new();

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

        public void RegisterFileTypeHandler(IFileTypeHandler handler)
        {
            if (!file_type_handlers_.Contains(handler)) file_type_handlers_.Add(handler);
        }

        protected IEnumerable<IFileTypeHandler> GetFileTypeHandlers()
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
                    if (handler is IAsyncFileTypeHandler async_handler)
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
                    if (handler is IAsyncFileTypeHandler async_handler)
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

    /// <summary>
    /// Provides an abstract base class for binary serializers with integrated file storage capabilities,
    /// enabling serialization and deserialization to and from binary formats, as well as handling file operations.
    /// </summary>
    /// <typeparam name="TConverter">The type of the converter used for transforming data during serialization.</typeparam>
    /// <typeparam name="TOptions">The type of the configuration options used to customize the serializer behavior.</typeparam>
    public abstract class
        BinarySerializerWithStorage<TConverter, TOptions> : AsyncSerializerBase<TConverter, TOptions>,
        IBinarySerializer, IFileStorage
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

        private readonly List<IFileTypeHandler> file_type_handlers_ = new();
        private IFileStorage file_storage_implementation_;

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
        public void RegisterFileTypeHandler(IFileTypeHandler handler)
        {
            if (!file_type_handlers_.Contains(handler)) file_type_handlers_.Add(handler);
        }

        /// <summary>
        /// 获取所有文件类型处理器
        /// </summary>
        protected IEnumerable<IFileTypeHandler> GetFileTypeHandlers()
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
                    if (handler is IAsyncFileTypeHandler async_handler)
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
                    if (handler is IAsyncFileTypeHandler async_handler)
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