namespace CnCNet.LauncherStub;

using System.Runtime.InteropServices;

/// <summary>
/// Windows 本地 API 互操作：P/Invoke 声明和错误码常量
/// </summary>
internal static class NativeInterop
{
    #region Windows 错误码常量

    /// <summary>ERROR_FILE_NOT_FOUND -> 2L</summary>
    public const int ERROR_FILE_NOT_FOUND = 2;

    /// <summary>ERROR_ACCESS_DENIED -> 5L</summary>
    public const int ERROR_ACCESS_DENIED = 5;

    #endregion

    #region kernel32.dll P/Invoke

    /// <summary>
    /// 删除指定文件。用于绕过 .NET 的文件删除限制（如长路径等场景）
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteFile(string name);

    #endregion
}
