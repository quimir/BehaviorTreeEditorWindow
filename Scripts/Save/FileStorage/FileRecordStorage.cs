using System;
using System.Collections.Generic;
using System.Linq;
using Save.Serialization.Core.TypeConverter.SerializerAttribute;
using UnityEngine;

namespace Save.FileStorage
{
    /// <summary>
    /// Represents a storage for managing file records with functionality to add, update, remove, and set the current file record.
    /// The storage is designed to handle a predefined maximum number of file records.
    /// </summary>
    [CustomSerialize]
    [Serializable]
    public class FileRecordStorage
    {
        /// <summary>
        /// Represents a private collection of file records managed by the FileRecordStorage class.
        /// The list stores instances of FileRecord and is serialized with persistence and custom
        /// serialization attributes.
        /// </summary>
        [PersistField] private List<FileRecord> file_records_ = new();

        /// <summary>
        /// Represents a private field containing the currently active FileRecord in the FileRecordStorage class.
        /// This field is marked with the PersistField attribute for serialization, allowing its value to be persisted.
        /// It is updated when the current file record changes, such as during file additions, updates, or selections.
        /// </summary>
        [PersistField] private FileRecord current_file_record_;

        /// <summary>
        /// Specifies the maximum number of file records that can be stored in the FileRecordStorage instance.
        /// This value is serialized for persistence and can be modified at runtime.
        /// </summary>
        [PersistField] private int max_file_count_ = 10;

        /// <summary>
        /// Provides access to the current file record managed by the FileRecordStorage class.
        /// This property retrieves the FileRecord instance that is marked as the active or selected file.
        /// It is excluded from serialization and directly reflects the state of the private field `current_file_record_`.
        /// </summary>
        [NonSerialize]
        public FileRecord CurrentFileRecord => current_file_record_;

        /// <summary>
        /// Provides read-only access to the collection of file records stored by the FileRecordStorage.
        /// This property returns an IReadOnlyList of FileRecord instances, ensuring the collection
        /// cannot be modified externally while allowing iteration and access.
        /// </summary>
        [NonSerialize]
        public IReadOnlyList<FileRecord> FileRecords => file_records_;

        public FileRecordStorage(){}

        public FileRecordStorage(int max_file_count=10)
        {
            max_file_count_ = max_file_count;
        }

        /// <summary>
        /// Sets the maximum number of file records that can be stored in the FileRecordStorage instance.
        /// Updates the storage to ensure compliance with the new maximum limit.
        /// </summary>
        /// <param name="max_file_count">The new maximum number of file records allowed in the storage.</param>
        public void SetMaxFileCount(int max_file_count)
        {
            max_file_count_ = max_file_count;
            UpdateRemoveRecord();
        }

        /// <summary>
        /// Adds a new file record or updates an existing one with the specified file path.
        /// Optionally sets the specified file as the current file record.
        /// </summary>
        /// <param name="file_path">The file path for the record to be added or updated.</param>
        /// <param name="make_current_file">A boolean flag indicating whether to set the specified file as the current
        /// file record. Defaults to true.</param>
        public void AddOrUpdateFile(string file_path, bool make_current_file = true)
        {
            if (string.IsNullOrEmpty(file_path)) return;
            
            // 检查是否已存在
            var existing_file=file_records_.FirstOrDefault(f=>f.FullPath==file_path);

            if (existing_file!=null)
            {
                existing_file.UpdateAccessTime();
                if (make_current_file)
                {
                    current_file_record_ = existing_file;
                }
                    
                return;
            }
                
            // 创建新记录
            var file_record = new FileRecord(file_path);
            file_records_.Add(file_record);
                
            UpdateRemoveRecord();

            if (make_current_file)
            {
                current_file_record_=file_record;
            }
        }

        /// <summary>
        /// Adds a new file record to the storage or updates an existing one based on the provided file path.
        /// </summary>
        /// <param name="file_path">The path of the file to be added or updated within the storage.</param>
        /// <param name="old_file_path">The previous file path, if the file is being updated. Can be null or empty for
        /// adding new files.</param>
        /// <param name="make_current_file">Indicates whether the provided file should be set as the current active
        /// file. Defaults to true.</param>
        public void AddOrUpdateFile(string file_path, string old_file_path, bool make_current_file = true)
        {
            if (string.IsNullOrEmpty(file_path))
            {
                return;
            }
            
            FileRecord target_file=null;
            
            // 如果提供了旧路径，直接查找并更新
            if (!string.IsNullOrEmpty(old_file_path))
            {
                target_file=file_records_.FirstOrDefault(f=>f.FullPath==old_file_path);

                if (target_file!=null)
                {
                    target_file.UpdateFileRecord(file_path);
                    if (make_current_file)
                    {
                        current_file_record_ = target_file;
                    }
                    
                    return;
                }
            }
            
            target_file=file_records_.FirstOrDefault(f=>f.FullPath==file_path);
            if (target_file!=null)
            {
                target_file.UpdateAccessTime();
                if (make_current_file)
                {
                    current_file_record_ = target_file;
                }
                
                return;
            }
        }

        /// <summary>
        /// Sets the file specified by the provided file path as the current active file in the FileRecordStorage.
        /// If the file does not exist in storage, it will add or update the file records and set it as the current file.
        /// </summary>
        /// <param name="file_path">The full path of the file to be set as the current file.</param>
        public void SetCurrentFile(string file_path)
        {
            if (string.IsNullOrEmpty(file_path))
            {
                return;
            }
            
            var target_file=file_records_.FirstOrDefault(f=>f.FullPath==file_path);
            
            if (target_file!=null)
            {
                current_file_record_=target_file;
                target_file.UpdateAccessTime();
                return;
            }
            
            AddOrUpdateFile(file_path,true);
            current_file_record_ = file_records_.FirstOrDefault(f => f.FullPath == file_path);
        }

        /// <summary>
        /// Removes the specified file record from the storage.
        /// If the file being removed is the current file record, updates the current record
        /// to the most recently accessed file in the storage.
        /// </summary>
        /// <param name="file_path">The full path of the file record to be removed from the storage.</param>
        /// <returns>Returns true if the file record was successfully removed and the current file record was updated,
        /// otherwise false.</returns>
        public bool RemoveFile(string file_path)
        {
            var file_to_remove = file_records_.FirstOrDefault(f =>f.FullPath==file_path);
            if (file_to_remove!=null)
            {
                var remove_to = file_records_.Remove(file_to_remove);
                
                // 如果移除的是当前文件，重新设置当前文件
                if (current_file_record_==file_to_remove)
                {
                    current_file_record_ = file_records_.OrderByDescending(f => f.LastAccessTime).FirstOrDefault();
                }

                return remove_to && current_file_record_ != null;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the full path of a file record based on its display name.
        /// Searches within the managed collection of file records in the storage.
        /// </summary>
        /// <param name="display_name">The display name of the file record to locate.</param>
        /// <returns>The full path of the file record if found; otherwise, null.</returns>
        public string GetFullPathByDisplayName(string display_name)
        {
            var file = file_records_.FirstOrDefault(f=>f.DisplayName==display_name);
            return file?.FullPath;
        }

        /// <summary>
        /// Clears all file records from the FileRecordStorage instance and resets the current file record to null.
        /// This effectively empties the storage and removes any active file association.
        /// </summary>
        public void Clear()
        {
            file_records_.Clear();
            current_file_record_=null;
        }

        /// <summary>
        /// Ensures that the number of file records in the storage does not exceed the predefined maximum limit.
        /// Removes the oldest file record, except for the current file record, if the limit is exceeded.
        /// </summary>
        private void UpdateRemoveRecord()
        {
            // 检查是否超出最大数量限制
            if (file_records_.Count>max_file_count_)
            {
                // 移除最旧文件除了当前文件
                var oldest_file = file_records_.Where(f => f != current_file_record_).OrderBy(f => 
                        f.LastAccessTime)
                    .FirstOrDefault();

                if (oldest_file!=null)
                {
                    file_records_.Remove(oldest_file);
                }
            }
        }
    }
}
