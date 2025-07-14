using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Save.FileStorage
{
    #region 调用系统文件系统用以保存和打开

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class FileDlg
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter = null;
        public string customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public string file = null;
        public int maxFile = 0;
        public string fileTitle = null;
        public int maxFileTitle = 0;
        public string initialDir = null;
        public string title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public string defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }

    // 调用系统函数
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileDlg : FileDlg
    {
    }

    /// <summary>
    /// 打开文件
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class SaveFileDlg : FileDlg
    {
    }

    public class OpenFileDialog
    {
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In] [Out] OpenFileDlg ofd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int ShellExecute(IntPtr hwnd, string lpsz_op, string lpsz_file, string lpsz_params,
            string lpsz_dir, int fs_show_cmd);
    }

    public class SaveFileDialog
    {
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In] [Out] SaveFileDlg ofd);
    }

    public static class FileTool
    {
        public static string SaveFolderInProject(string file_extension)
        {
#if UNITY_EDITOR
            var save_path =
                EditorUtility.SaveFilePanelInProject("保存文件", "", file_extension.TrimStart('.'), "请指定文件保存位置");
            return save_path;
#endif
        }

        public static string SaveFolder(string file_extension, Action error = null)
        {
#if UNITY_EDITOR
            var save_path =
                EditorUtility.SaveFilePanel("保存文件", Application.dataPath, "", file_extension.TrimStart('.'));
            return save_path;
#else
 if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
            SaveFileDlg path = new SaveFileDlg();
            path.structSize = Marshal.SizeOf(path);
            path.dlgOwner = OpenFileDialog.GetForegroundWindow();
            path.filter = $"保存 (*{file_extension})\0*{file_extension}\0\0";
            path.file = new string(new char[256]);
            path.maxFile = path.file.Length;
            path.fileTitle = new string(new char[64]);
            path.maxFileTitle = path.fileTitle.Length;
            // 默认路径
            path.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            path.title = "保存文件";
            path.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
            if (SaveFileDialog.GetSaveFileName(path))
            {
                string file_path = path.file;
                return file_path;
            }
            else
            {
                if (error!=null)
                    error?.Invoke();
                return "";
            }
}
#endif
        }

        public static string OpenProject(string file_extension, Action error = null)
        {
#if UNITY_EDITOR
            var path = EditorUtility.OpenFilePanel("打开文件", Application.dataPath, file_extension.TrimStart('.'));
            return path;
#else
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
OpenFileDlg pth = new OpenFileDlg();
            pth.dlgOwner = OpenFileDialog.GetForegroundWindow();
            pth.structSize = Marshal.SizeOf(pth);
            pth.filter = $"加载 (*{file_extension})\0*{file_extension}\0\0";
            pth.file = new string(new char[256]);
            pth.maxFile = pth.file.Length;
            pth.fileTitle = new string(new char[64]);
            pth.maxFileTitle = pth.fileTitle.Length;
            pth.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); //默认路径
            pth.title = "打开文件";
            pth.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;

            if (OpenFileDialog.GetOpenFileName(pth))
            {
                string filepath = pth.file; //选择的文件路径;  
                return filepath;
            }
            else
            {
                if (error != null) error.Invoke();
                return "";
            }

}
#endif
        }

        public static void OpenExe(string file_path, string args)
        {
            OpenFileDialog.ShellExecute(IntPtr.Zero, "open", file_path, args, "", 1);
        }
    }

    #endregion
}