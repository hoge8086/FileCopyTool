using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FileCopy
{
    class FileUtil
    {
        private enum FileFuncFlags : uint
        {
            FO_MOVE = 0x1,
             FO_COPY = 0x2,
             FO_DELETE = 0x3,
             FO_RENAME = 0x4
        }

        [Flags]
        private enum FILEOP_FLAGS : ushort
        {
             FOF_MULTIDESTFILES = 0x1,
             FOF_CONFIRMMOUSE = 0x2,
             /// <summary>
             /// Don't create progress/report
             /// </summary>
             FOF_SILENT = 0x4,
             FOF_RENAMEONCOLLISION = 0x8,
             /// <summary>
             /// Don't prompt the user.
             /// </summary>
             FOF_NOCONFIRMATION = 0x10,
             /// <summary>
             /// Fill in SHFILEOPSTRUCT.hNameMappings.
             /// Must be freed using SHFreeNameMappings
             /// </summary>
             FOF_WANTMAPPINGHANDLE = 0x20,
             FOF_ALLOWUNDO = 0x40,
             /// <summary>
             /// On *.*, do only files
             /// </summary>
             FOF_FILESONLY = 0x80,
             /// <summary>
             /// Don't show names of files
             /// </summary>
             FOF_SIMPLEPROGRESS = 0x100,
             /// <summary>
             /// Don't confirm making any needed dirs
             /// </summary>
             FOF_NOCONFIRMMKDIR = 0x200,
             /// <summary>
             /// Don't put up error UI
             /// </summary>
             FOF_NOERRORUI = 0x400,
             /// <summary>
             /// Dont copy NT file Security Attributes
             /// </summary>
             FOF_NOCOPYSECURITYATTRIBS = 0x800,
             /// <summary>
             /// Don't recurse into directories.
             /// </summary>
             FOF_NORECURSION = 0x1000,
             /// <summary>
             /// Don't operate on connected elements.
             /// </summary>
             FOF_NO_CONNECTED_ELEMENTS = 0x2000,
             /// <summary>
             /// During delete operation, 
             /// warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
             /// </summary>
             FOF_WANTNUKEWARNING = 0x4000,
             /// <summary>
             /// Treat reparse points as objects, not containers
             /// </summary>
             FOF_NORECURSEREPARSE = 0x8000
        }

        //[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        //If you use the above you may encounter an invalid memory access exception (when using ANSI
        //or see nothing (when using unicode) when you use FOF_SIMPLEPROGRESS flag.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
             public FileFuncFlags wFunc;
             [MarshalAs(UnmanagedType.LPWStr)]
             public string pFrom;
             [MarshalAs(UnmanagedType.LPWStr)]
             public string pTo;
             public FILEOP_FLAGS fFlags;
             [MarshalAs(UnmanagedType.Bool)]
             public bool fAnyOperationsAborted;
             public IntPtr hNameMappings;
             [MarshalAs(UnmanagedType.LPWStr)]
             public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

        //SHFileOperationを使ってファイル一括コピー(ワイルドカード使用可能)
        public static bool Copy(string[] from, string[] to)
        {
            if (from.Length == 0 || to.Length == 0)
                return true;

            if(from.Length != to.Length)
                return false;

            SHFILEOPSTRUCT shfos;
            shfos.hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            shfos.wFunc = FileFuncFlags.FO_COPY;
            shfos.pFrom = string.Join("\0", from) + "\0\0";
            shfos.pTo = string.Join("\0", to) + "\0\0";
            //フラグ：UNDO可、コピー先複数可、フォルダ作成時確認不要
            shfos.fFlags = FILEOP_FLAGS.FOF_ALLOWUNDO | FILEOP_FLAGS.FOF_MULTIDESTFILES | FILEOP_FLAGS.FOF_NOCONFIRMMKDIR;
            shfos.fAnyOperationsAborted = true;
            shfos.hNameMappings = IntPtr.Zero;
            shfos.lpszProgressTitle = null;

            if (SHFileOperation(ref shfos) != 0)
                return false;

            return true;

        }

        //コピー元パスからコピー先のパスを生成する
        public static bool MakeDstPath(string fromPath, string toFolderPath, ref string dstPath, string fromBaseFolderPath = null)
        {
            //引数のチェック(パスはすべて絶対パス)
            if ((fromPath == null || toFolderPath == null) ||
                (!System.IO.Path.IsPathRooted(fromPath) || !System.IO.Path.IsPathRooted(toFolderPath)) ||
                (fromBaseFolderPath != null && !System.IO.Path.IsPathRooted(fromBaseFolderPath)))
            {
                dstPath = null;
                return false;
            }

            if (!toFolderPath.EndsWith(@"\"))
                toFolderPath += @"\";

            if (fromBaseFolderPath != null && !fromBaseFolderPath.EndsWith(@"\"))
                fromBaseFolderPath += @"\";

            if(fromBaseFolderPath != null && fromPath.StartsWith(fromBaseFolderPath))
            {
                //コピー元が起点フォルダ配下の場合
                //コピー先は起点フォルダ配下のディレクトリ構造を保持する
                dstPath = toFolderPath + fromPath.Substring(fromBaseFolderPath.Length);
            }
            else
            {
                //上記以外
                //コピー先フォルダ直下がコピー先
                 dstPath = toFolderPath + System.IO.Path.GetFileName(fromPath);
            }

            //ワイルドカードが使用されている場合は、コピー先はフォルダにする(ファイルではなく)
            if (System.IO.Path.GetFileName(dstPath).IndexOf("*") >= 0)
            {
                dstPath = System.IO.Path.GetDirectoryName(dstPath);
            }

            return true;
        }
    }
}
