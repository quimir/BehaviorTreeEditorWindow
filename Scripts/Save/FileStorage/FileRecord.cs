using System;
using System.IO;
using System.Linq;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;

namespace Save.FileStorage
{
    /// <summary>
    /// Represents a file record with properties for file path, display name, file name, creation time, and last access time.
    /// Allows for updating file records with new paths and updating the access time.
    /// </summary>
    [CustomSerialize]
    [Serializable]
    public class FileRecord
    {
        /// <summary>
        /// Represents the full file path associated with a file record.
        /// This field is marked for persistence using the
        /// <see cref="Save.Serialization.Core.TypeConverter.SerializerAttribute.PersistFieldAttribute"/>.
        /// It is a private string that contains the complete path to the file stored in the file record.
        /// </summary>
        [PersistField] private string full_path_;

        /// <summary>
        /// Represents the truncated display name of a file derived from its full path.
        /// This field is marked for persistence using the
        /// <see cref="Save.Serialization.Core.TypeConverter.SerializerAttribute.PersistFieldAttribute"/>.
        /// It is a private string that is initialized based on the file's full path
        /// and is used as a more user-friendly identifier for the file record.
        /// </summary>
        [PersistField] private string display_name_;

        /// <summary>
        /// Provides the full file path of the associated file record.
        /// This property is derived from the private field `full_path_`
        /// and is marked with the <see cref="Save.Serialization.Core.TypeConverter.SerializerAttribute.NonSerializeAttribute"/>
        /// to prevent it from being serialized.
        /// It is used for accessing the complete path of the file represented by the file record.
        /// </summary>
        [NonSerialize]
        public string FullPath => full_path_;

        /// <summary>
        /// Gets the display name associated with a file record.
        /// This property is derived from the private field marked with the
        /// <see cref="Save.Serialization.Core.TypeConverter.SerializerAttribute.NonSerializeAttribute"/>
        /// It serves as a human-readable identifier for the file record and is intended to
        /// provide a meaningful name for presentation or user interface display purposes.
        /// </summary>
        [NonSerialize]
        public string DisplayName => display_name_;

        /// <summary>
        /// Provides the name of the file derived from the full file path.
        /// This property utilizes <see cref="System.IO.Path.GetFileName(string)"/> to extract
        /// the name of the file, including its extension, from the stored full path.
        /// It is a non-serialized property and is calculated dynamically based on the
        /// private field `full_path_`.
        /// </summary>
        [NonSerialize]
        public string FileName => Path.GetFileName(full_path_);

        /// <summary>
        /// Represents the creation time of the file record.
        /// This property is initialized with the system's current time upon instantiation of the <see cref="FileRecord"/> object.
        /// Its value remains constant after initialization and reflects when the file record was created.
        /// </summary>
        public DateTime CreateTime { get; }

        /// <summary>
        /// Represents the timestamp of the most recent access to the file represented by this record.
        /// This property is updated whenever the file is accessed or otherwise interacted with.
        /// It is a <see cref="System.DateTime"/> value and is initialized to the current time when the
        /// file record is created.
        /// </summary>
        public DateTime LastAccessTime { get; private set; }
        
        public FileRecord()
        {}

        public FileRecord(string full_path)
        {
            full_path_ = full_path??throw new ArgumentNullException(nameof(full_path));
            display_name_ = TruncatePath(full_path);
            CreateTime=DateTime.Now;
            LastAccessTime=DateTime.Now;
        }

        /// <summary>
        /// Updates the last access time of the file record to the current system date and time.
        /// </summary>
        public void UpdateAccessTime()
        {
            LastAccessTime=DateTime.Now;
        }

        /// <summary>
        /// Updates the file record with a new file path, adjusts the display name, and updates the last access time.
        /// </summary>
        /// <param name="new_path">The new file path to update the file record with.</param>
        public void UpdateFileRecord(string new_path)
        {
            full_path_ = new_path;
            display_name_=TruncatePath(new_path);
            UpdateAccessTime();
        }

        /// <summary>
        /// Shortens the provided file path to a specified maximum length while preserving
        /// the root directory and file name. If necessary, parts of the path are replaced with ellipses ("...")
        /// to fit within the specified length.
        /// </summary>
        /// <param name="path">The full file path to be truncated.</param>
        /// <param name="max_length">The maximum allowable length of the truncated path. Defaults to 20.</param>
        /// <returns>The truncated file path if the original exceeds the maximum length; otherwise, the original path.</returns>
        private static string TruncatePath(string path, int max_length = 20)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= max_length)
            {
                return path;
            }

            var file_name = Path.GetFileName(path);
            var directory = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(directory))
            {
                return file_name;
            }
            
            // 获取根目录（第一个目录）
            var path_parts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var root_dir = path_parts.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

            if (string.IsNullOrEmpty(root_dir))
            {
                return file_name;
            }
            
            var truncated_path = $"{root_dir}{Path.DirectorySeparatorChar}...{Path.DirectorySeparatorChar}{file_name}";

            if (truncated_path.Length>max_length && file_name.Length>10)
            {
                var max_file_name_length = max_length - 6;
                if (max_file_name_length>0)
                {
                    var truncated_file_name = file_name.Length > max_file_name_length
                        ? file_name.Substring(0, max_file_name_length - 3) + "..."
                        : file_name;
                    truncated_path = $"{root_dir}{Path.DirectorySeparatorChar}...{Path.DirectorySeparatorChar}{truncated_file_name}";
                }
            }

            return truncated_path;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
