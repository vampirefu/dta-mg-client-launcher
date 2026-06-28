namespace CnCNet.LauncherStub;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

/// <summary>
/// CnCNet 客户端启动器主程序。
/// 负责检测运行环境（.NET 运行时）并启动 WindowsDX 客户端
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// 目标 .NET 运行时主版本号，用于检测和下载链接拼接
    /// </summary>
    private const int DotNetMajorVersion = 8;

    /// <summary>
    /// .NET 客户端 DLL 所在的子目录名
    /// </summary>
    private const string DotNetBinariesFolder = "Binaries";

    /// <summary>
    /// 客户端 DLL 所在的渲染子目录名
    /// </summary>
    private const string RenderFolder = "Windows";

    /// <summary>
    /// 客户端 DLL 文件名
    /// </summary>
    private const string ClientDllName = "clientdx.dll";

    /// <summary>
    /// 启动器自身所在目录的完整路径
    /// </summary>
    private static readonly string CurrentDirectory = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName;

    /// <summary>
    /// 当前操作系统版本，启动时一次性检测
    /// </summary>
    public static OSVersion CurrentOSVersion { get; } = GetOperatingSystemVersion();

    #region .NET 下载链接
    private static readonly Uri DotNetDownloadLink = new("https://dotnet.microsoft.com/download");
    private static readonly Uri DotNetX64DesktopRuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-x64.exe");
    private static readonly Uri DotNetX86DesktopRuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-x86.exe");
    private static readonly Uri DotNetArm64DesktopRuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-arm64.exe");
    #endregion

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            // 解析命令行参数
            foreach (string arg in args)
            {
                switch (arg.ToUpperInvariant())
                {
                    case "-DX":
                        RunDX();
                        return;
                    case "-DIALOGTEST":
                        RunDialogTest();
                        return;
                }
            }

            // 无参数时直接启动 DX 版本
            RunDX();
        }
        catch (Exception ex)
        {
            AdvancedMessageBox.ShowOkMessageBox(ex.ToString(), "客户端启动器错误", okText: "退出");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// 对话框测试模式，用于调试 AdvancedMessageBox 的各种场景
    /// </summary>
    private static void RunDialogTest()
    {
        var msgbox = new AdvancedMessageBox();
        var model = (AdvancedMessageBoxViewModel)msgbox.DataContext;
        model.Title = "客户端启动器对话框测试";
        model.Message = "点击下方按钮进行测试。";
        model.Commands = new ObservableCollection<CommandViewModel>()
        {
            new CommandViewModel()
            {
                Text = "显示缺失组件对话框",
                Command = new RelayCommand(_ => ShowMissingComponent("组件名称", new Uri("https://github.com/CnCNet/dta-mg-client-launcher"))),
            },
            new CommandViewModel()
            {
                Text = "抛出异常",
                Command = new RelayCommand(_ => throw new Exception("异常消息")),
            },
            new CommandViewModel()
            {
                Text = "退出",
                Command = new RelayCommand(_ => msgbox.Close()),
            },
        };
        msgbox.ShowDialog();
    }

    /// <summary>
    /// 以 DirectX11 渲染模式启动客户端（64位，需要桌面运行时）
    /// </summary>
    private static void RunDX()
    {
        StartProcessDotNet(BuildDllPath(ClientDllName));
    }

    /// <summary>
    /// 拼接客户端 DLL 的相对路径：Resources\Binaries\Windows\clientdx.dll
    /// </summary>
    private static string BuildDllPath(string dllName)
        => $"Resources{Path.DirectorySeparatorChar}{DotNetBinariesFolder}{Path.DirectorySeparatorChar}{RenderFolder}{Path.DirectorySeparatorChar}{dllName}";

    /// <summary>
    /// 拼接当前目录下的绝对路径
    /// </summary>
    private static string ToAbsolutePath(string relativePath)
        => CurrentDirectory + Path.DirectorySeparatorChar + relativePath;

    /// <summary>
    /// 操作系统版本枚举
    /// </summary>
    public enum OSVersion
    {
        /// <summary>不支持 .NET 8 的旧系统（XP/Vista 等）</summary>
        LEGACY,
        WIN7,
        WIN8,
        WIN1011,
    }

    /// <summary>
    /// 根据系统版本号判断当前操作系统版本。
    /// .NET 8 仅支持 Win7+，旧系统统一归为 LEGACY
    /// </summary>
    private static OSVersion GetOperatingSystemVersion()
    {
        Version osVersion = Environment.OSVersion.Version;

        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            return OSVersion.LEGACY;

        // Win10/11: Major >= 10
        if (osVersion.Major >= 10)
            return OSVersion.WIN1011;

        // Win8/8.1: 6.2+
        if (osVersion.Major == 6 && osVersion.Minor >= 2)
            return OSVersion.WIN8;

        // Win7: 6.1
        if (osVersion.Major == 6 && osVersion.Minor == 1)
            return OSVersion.WIN7;

        // 更旧的系统（XP/Vista 等）
        return OSVersion.LEGACY;
    }

    /// <summary>
    /// 使用 .NET 运行时启动指定路径的客户端 DLL
    /// </summary>
    /// <param name="relativePath">客户端 DLL 的相对路径</param>
    private static void StartProcessDotNet(string relativePath)
    {
        bool run32Bit = !Environment.Is64BitOperatingSystem;
        string architecture = run32Bit ? "x86" : GetMachineArchitecture();

        // 检查 .NET 运行时是否已安装，获取 dotnet host 路径
        string dotnetHost = CheckAndRetrieveDotNetHost(architecture);
        string absolutePath = ToAbsolutePath(relativePath);

        if (!File.Exists(absolutePath))
        {
            AdvancedMessageBox.ShowOkMessageBox($"未找到客户端主程序库 ({relativePath})！", "客户端启动器错误", okText: "退出");
            Environment.Exit(3);
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = dotnetHost,
            Arguments = "\"" + absolutePath + "\"",
#if DEBUG
            CreateNoWindow = false,
            UseShellExecute = false,
            RedirectStandardError = true,
#else
            CreateNoWindow = true,
            UseShellExecute = false,
#endif
        };

        // 允许 .NET 运行时向前兼容：即使 DLL 目标是低版本 .NET，也能在已安装的高版本上运行
        processStartInfo.EnvironmentVariables["DOTNET_ROLL_FORWARD"] = "LatestMajor";

        // Win7 需要禁用 W^X 安全策略，否则 .NET 运行时会出错
        if (CurrentOSVersion == OSVersion.WIN7)
            processStartInfo.EnvironmentVariables["DOTNET_EnableWriteXorExecute"] = "0";

#if DEBUG
        using var process = Process.Start(processStartInfo);
        if (process != null && process.WaitForExit(5000))
        {
            string error = process.StandardError.ReadToEnd();
            AdvancedMessageBox.ShowOkMessageBox(
                $"客户端进程意外退出（退出码：{process.ExitCode}）！\n\n" +
                $"dotnet: {dotnetHost}\nDLL: {absolutePath}\n\n" +
                $"错误输出：\n{error}",
                "客户端启动器错误", okText: "退出");
        }
#else
        Process.Start(processStartInfo);
#endif
    }

    /// <summary>
    /// 检查指定架构的 .NET Desktop Runtime 是否已安装，返回 dotnet host 的完整路径；
    /// 未安装则提示下载并退出程序
    /// </summary>
    private static string CheckAndRetrieveDotNetHost(string architecture)
    {
        // DX 版本始终需要 Desktop Runtime
        bool installed = IsDotNetDesktopInstalled(architecture);

        if (!installed)
        {
            string missingComponent = $".NET Desktop Runtime 版本 {DotNetMajorVersion}（架构：{architecture}）";
            ShowMissingComponent(missingComponent, GetDotNetDownloadLink(architecture));
            Environment.Exit(2);
            return null;
        }

        // 从注册表获取 dotnet host 安装路径
        using var localMachine32BitRegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using RegistryKey? dotnetArchitectureKey = localMachine32BitRegistryKey.OpenSubKey(
            $"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\{architecture}");
        string? installLocation = dotnetArchitectureKey?.GetValue("InstallLocation")?.ToString();

        if (installLocation is null)
        {
            ShowMissingComponent($".NET Runtime 版本 {DotNetMajorVersion}", DotNetDownloadLink);
            Environment.Exit(2);
            return null;
        }

        return new FileInfo(installLocation + Path.DirectorySeparatorChar + "dotnet.exe").FullName;
    }

    /// <summary>
    /// 获取当前进程的 CPU 架构（x64/x86/arm64）
    /// </summary>
    private static string GetMachineArchitecture()
        => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    /// <summary>
    /// 根据架构返回对应的 Desktop Runtime 下载链接
    /// </summary>
    private static Uri GetDotNetDownloadLink(string architecture)
    {
        return architecture.ToLowerInvariant() switch
        {
            "x64" => DotNetX64DesktopRuntimeDownloadLink,
            "x86" => DotNetX86DesktopRuntimeDownloadLink,
            "arm64" => DotNetArm64DesktopRuntimeDownloadLink,
            _ => DotNetDownloadLink,
        };
    }

    /// <summary>
    /// 使用默认浏览器打开指定链接
    /// </summary>
    private static void OpenUri(Uri uri)
    {
        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true,
        });
    }

    /// <summary>
    /// 显示缺失组件的提示对话框，用户可选择打开下载链接或退出
    /// </summary>
    private static void ShowMissingComponent(string missingComponent, Uri downloadLink)
    {
        bool dialogResult = AdvancedMessageBox.ShowYesNoMessageBox(
            string.Format(
            "缺少组件 {0}。\n\n" +
            "您可以从以下链接下载安装程序：\n\n{1}",
            missingComponent,
            downloadLink.ToString()),
            "缺少组件",
            yesText: "打开链接", noText: "退出");
        if (dialogResult)
            OpenUri(downloadLink);
    }

    /// <summary>
    /// 检查指定架构的 .NET Desktop Runtime 是否已安装
    /// </summary>
    private static bool IsDotNetDesktopInstalled(string architecture)
        => IsDotNetInstalled(architecture, "Microsoft.WindowsDesktop.App");

    /// <summary>
    /// 通过注册表检查指定架构和框架名的 .NET 运行时是否已安装。
    /// 查找注册表中主版本号 >= 目标版本、不含预发布标识（'-'）且标记为已安装（值为"1"）的条目。
    /// 由于设置了 DOTNET_ROLL_FORWARD=LatestMajor，高版本运行时可以兼容低版本 DLL
    /// </summary>
    private static bool IsDotNetInstalled(string architecture, string sharedFrameworkName)
    {
        using var localMachine32BitRegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using RegistryKey? dotnetSharedFrameworkKey = localMachine32BitRegistryKey.OpenSubKey(
            $"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\{architecture}\\sharedfx\\{sharedFrameworkName}");

        if (dotnetSharedFrameworkKey == null)
            return false;

        return dotnetSharedFrameworkKey.GetValueNames().Any(q =>
        {
            if (q.Contains('-'))
                return false;

            // 解析版本号，检查主版本是否 >= 目标版本
            if (!int.TryParse(q.Split('.')[0], out int majorVersion))
                return false;

            return majorVersion >= DotNetMajorVersion
                && "1".Equals(dotnetSharedFrameworkKey.GetValue(q)?.ToString(), StringComparison.OrdinalIgnoreCase);
        });
    }
}
