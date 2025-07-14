using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using ExTools;

namespace Save.Serialization.Core.FileStorage
{
    public abstract class SerializerFileStorageBase : ISerializerFileStorage
    {
        private readonly List<ISerializerFileTypeHandler> file_type_handlers_ = new();

        public abstract string[] GetSupportedFileExtensions();

        public virtual bool CanHandleFile(string file_path, bool read_content = false)
        {
            if (!System.IO.File.Exists(file_path)) return false;

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

        public void RegisterFileTypeHandler(ISerializerFileTypeHandler handler)
        {
            if (!file_type_handlers_.Contains(handler)) file_type_handlers_.Add(handler);
        }

        protected virtual bool CanHandleFileContent(byte[] header)
        {
            // 默认不基于判断，由子类覆盖实现特定逻辑
            return false;
        }

        protected IEnumerable<ISerializerFileTypeHandler> GetFileTypeHandlers()
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

    public abstract class TextSerializerFileStorage : SerializerFileStorageBase
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
            System.IO.File.WriteAllText(file_path, content);
        }

        public override T LoadFromFile<T>(string file_path)
        {
            if (!System.IO.File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文件类型处理
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.TryLoadFromFile(this, file_path, out T result))
                        return result;

            // 默认文本加载
            var content = System.IO.File.ReadAllText(file_path);
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
                    if (handler is IAsyncSerializerFileTypeHandler async_handler)
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
                System.IO.File.WriteAllText(file_path, content);
            }, cancellationToken: cancellation_token);
        }
    }

    public abstract class BinarySerializerFileStorage : SerializerFileStorageBase
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
            System.IO.File.WriteAllBytes(file_path, content);
        }

        public override T LoadFromFile<T>(string file_path)
        {
            if (!System.IO.File.Exists(file_path)) return default;

            var extension = Path.GetExtension(file_path).ToLowerInvariant();

            // 尝试使用文件类型处理器
            foreach (var handler in GetFileTypeHandlers())
                if (Array.Exists(handler.GetSupportedExtensions(),
                        evt => (evt.StartsWith(".") ? evt : "." + evt).Equals(extension,
                            StringComparison.OrdinalIgnoreCase)))
                    if (handler.TryLoadFromFile(this, file_path, out T result))
                        return result;

            // 默认二进制加载
            var content = System.IO.File.ReadAllBytes(file_path);
            return DeserializeFromBinary<T>(content);
        }
    }
}