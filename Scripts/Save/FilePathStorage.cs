using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Utillties;
using UnityEngine;

namespace Save
{
    /// <summary>
    /// Provides functionality for managing and storing file path information, including
    /// the last opened file path, current opened file path, and a history of recently opened files.
    /// Can optionally save and load this information in JSON or binary formats.
    /// </summary>
    public class FilePathStorage
    {
        // 上一次打开的文件路径
        private string last_opened_file_path_;

        public string LastOpenedFilePath
        {
            get => last_opened_file_path_;
            set => last_opened_file_path_ = value;
        }

        // 当前打开的文件路径
        private string current_opened_file_path_;

        /// <summary>
        /// Gets or sets the path of the file currently opened in the application.
        /// This property maintains the current file path being worked on, allowing it
        /// to be accessed or updated. When modified, it can be used to track the
        /// active file in conjunction with other file management features.
        /// </summary>
        public string CurrentOpenedFilePath
        {
            get => current_opened_file_path_;
            set => current_opened_file_path_ = value;
        }

        public List<string> LastTenOpenedFilePaths
        {
            get => last_ten_opened_file_paths_;
            set => last_ten_opened_file_paths_ = value;
        }

        // 最近十次打开的文件路径（最新的排在最前）
        private List<string> last_ten_opened_file_paths_ = new List<string>();
    
        // 是否启用自动保存
        private bool is_auto_save_enabled_;
        
        private bool use_json_;

        public bool UseJson
        {
            get => use_json_;
            set => use_json_ = value;
        }

        public FilePathStorage(bool use_json=true)
        {
            use_json_ = use_json;
        }

        /// <summary>
        /// 更新文件路径信息: 将当前路径移到 LastOpenedFilePath 并更新当前路径，同时维护最近十次记录
        /// </summary>
        /// <param name="file_path">文件路径</param>
        public void SaveFilePath(string file_path)
        {
            if (string.IsNullOrWhiteSpace(file_path))
            {
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(current_opened_file_path_))
            {
                last_opened_file_path_ = current_opened_file_path_;
            }

            current_opened_file_path_ = file_path;
        
            // 避免重复记录
            last_ten_opened_file_paths_.Remove(file_path);
            // 在列表最前面插入
            last_ten_opened_file_paths_.Insert(0,file_path);
            // 保证记录数不超过x条
            if (last_ten_opened_file_paths_.Count>FixedValues.kMaxFilePaths)
            {
                last_ten_opened_file_paths_.RemoveRange(FixedValues.kMaxFilePaths,last_ten_opened_file_paths_.Count-FixedValues.kMaxFilePaths);
            }
        }

        public FilePathStorage LoadFilePath()
        {
            return use_json_ ? LoadFromJson() : LoadFromBinary();
        }

        public void SaveFilePath()
        {
            if (use_json_)
            {
                SaveAsJson();
            }
            else
            {
                SaveAsBinary();
            }
        }

        public void ClearCurrentFilePath()
        {
            current_opened_file_path_ = null;
        }

        private string GetFileDefaultPath()
        {
            return Path.Combine(Application.persistentDataPath, use_json_ ? "BtWindowsFile.json" : "BtWindowsFile.dat");
        }

        private void SaveAsJson()
        {
            // 使用Newtonsoft.json进行序列化
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(GetFileDefaultPath(),json);
        }

        private FilePathStorage LoadFromJson()
        {
            string path = GetFileDefaultPath();
            if (!File.Exists(path))
            {
                Debug.LogWarning("Json文件不存在，返回默认实例化");
                return new FilePathStorage();
            }

            string json = File.ReadAllText(path);
            FilePathStorage storage = JsonConvert.DeserializeObject<FilePathStorage>(json);
            return storage;
        }

        private void SaveAsBinary()
        {
            using var fs = new FileStream(GetFileDefaultPath(), FileMode.Create);
            using var writer=new BinaryWriter(fs);
            writer.Write(last_opened_file_path_??"");
            writer.Write(current_opened_file_path_??"");
            writer.Write(last_ten_opened_file_paths_.Count);
            foreach (var path in last_ten_opened_file_paths_)
            {
                writer.Write(path??"");
            }
            writer.Write(is_auto_save_enabled_);
            
            Debug.Log($"文件保存到: {GetFileDefaultPath()}");
        }

        private FilePathStorage LoadFromBinary()
        {
            string path = GetFileDefaultPath();
            if (!File.Exists(path))
            {
                Debug.LogWarning("二进制文件不存在，返回默认实例");
                return new FilePathStorage();
            }

            FilePathStorage storage = new FilePathStorage();
            using var fs=new FileStream(path,FileMode.Open);
            using var reader=new BinaryReader(fs);
            storage.last_opened_file_path_ = reader.ReadString();
            storage.current_opened_file_path_ = reader.ReadString();
            int count = reader.ReadInt32();
            storage.last_ten_opened_file_paths_ = new List<string>();
            for (int i = 0; i < count; i++)
            {
                storage.last_ten_opened_file_paths_.Add(reader.ReadString());
            }

            storage.is_auto_save_enabled_ = reader.ReadBoolean();

            return storage;
        }
    }
}
