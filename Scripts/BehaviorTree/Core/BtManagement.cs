using System;
using System.IO;
using System.Linq;
using Save;
using Script.Save;
using Script.Tool;
using Script.Utillties;
using UnityEngine;

namespace Script.BehaviorTree.Save
{
    /// <summary>
    /// Provides management and utility functions related to the handling of Behavior Tree files
    /// including saving, clearing, and setting file paths, as well as handling associated formats and serializations.
    /// </summary>
    public static class BtManagement
    {
        public static FilePathStorage FilePathStorage = new FilePathStorage();

        static BtManagement()
        {
        }

        public static string GetCurrentFilePath()
        {
            return FilePathStorage?.CurrentOpenedFilePath;
        }

        public static void ClearCurrentFilePath()
        {
            FilePathStorage.ClearCurrentFilePath();
        }

        public static void SetCurrentFilePath(string path)
        {
            if (Path.GetExtension(path)==FixedValues.kBtDateFileExtension)
            {
                FilePathStorage.SaveFilePath(path);
                //Debug.Log($"Set current behavior tree file path: {file_path_storage_.CurrentOpenedFilePath}");
            }
            else
            {
                Debug.LogError($"Invalid file type. Expected {FixedValues.kBtDateFileExtension} file.");
            }
        }

        public static void SavePathStorage()
        {
            FilePathStorage.SaveFilePath();
        }
    }
}
