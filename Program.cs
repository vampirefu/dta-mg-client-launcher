﻿namespace CnCNet.LauncherStub;

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
/// 负责检测运行环境（.NET 运行时、XNA Framework）、选择渲染模式并启动客户端
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
    /// 启动器自身所在目录的完整路径
    /// </summary>
    private static readonly string CurrentDirectory = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.FullName;

    /// <summary>
    /// 当前操作系统版本，启动时一次性检测
    /// </summary>
    public static OSVersion CurrentOSVersion { get; } = GetOperatingSystemVersion();

    /// <summary>XNA Framework 4.0 Refresh 下载链接</summary>
    private static readonly Uri XnaDownloadLink = new("https://www.microsoft.com/download/details.aspx?id=27598");

    #region .NET 下载链接
    private static readonly Uri DotNetDownloadLink = new("https://dotnet.microsoft.com/download");
    private static readonly Uri DotNetX64DesktopRuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-x64.exe");
    private static readonly Uri DotNetX86DesktopRuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-x86.exe");
    private static readonly Uri DotNetArm64DesktopRuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/windowsdesktop-runtime-win-arm64.exe");
    private static readonly Uri DotNetX64RuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/dotnet-runtime-win-x64.exe");
    private static readonly Uri DotNetX86RuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/dotnet-runtime-win-x86.exe");
    private static readonly Uri DotNetArm64RuntimeDownloadLink = new($"https://aka.ms/dotnet/{DotNetMajorVersion}.0/dotnet-runtime-win-arm64.exe");
    #endregion

    /// <summary>
    /// 检查 XNA Framework 4.0 Refresh 是否已安装，未安装则提示下载并退出
    /// </summary>
    private static void RequireXna()
    {
        if (!IsXNAFramework4RefreshInstalled())
        {
            ShowMissingComponent("'Microsoft XNA Framework 4.0 Refresh'", XnaDownloadLink);
            Environment.Exit(2);
        }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            // 解析命令行参数，决定以哪种渲染模式启动客户端
            foreach (string arg in args)
            {
                switch (arg.ToUpperInvariant())
                {
                    case "-DX":
                        RunDX();
                        return;
                    case "-XNA":
                        RunXNA();
                        return;
                    case "-OGL":
                        RunOGL();
                        return;
                    case "-UGL":
                        RunUGL();
                        return;
                    case "-DIALOGTEST":
                        RunDialogTest();
                        return;
                }
            }

#if DEBUG
            MessageBox.Show("233");
#endif
            // 无参数时自动选择渲染模式
            AutoRun();
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
                Text = "显示 GPU 不兼容对话框",
                Command = new RelayCommand(_ => ShowIncompatibleGPUMessage(new[] { "打开链接（以下按钮均不可用）", "启动 XNA 版本", "启动 DirectX11 版本", "退出" })),
            },
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
    /// 以 XNA 渲染模式启动客户端（需要 XNA Framework 4.0 Refresh，32位，需要桌面运行时）
    /// </summary>
    private static void RunXNA()
    {
        RequireXna();
        StartProcessDotNet(BuildDllPath("XNA", "clientxna.dll"), run32Bit: true, runDesktop: true);
    }

    /// <summary>
    /// 以 OpenGL 渲染模式启动客户端（64位，需要桌面运行时）
    /// </summary>
    private static void RunOGL()
    {
        StartProcessDotNet(BuildDllPath("OpenGL", "clientogl.dll"), run32Bit: false, runDesktop: true);
    }

    /// <summary>
    /// 以 DirectX11 渲染模式启动客户端（64位，需要桌面运行时）
    /// </summary>
    private static void RunDX()
    {
        StartProcessDotNet(BuildDllPath("Windows", "clientdx.dll"), run32Bit: false, runDesktop: true);
    }

    /// <summary>
    /// 以 UniversalGL 渲染模式启动客户端（64位，不需要桌面运行时）
    /// </summary>
    private static void RunUGL()
    {
        StartProcessDotNet(BuildDllPath("UniversalGL", "clientogl.dll"), run32Bit: false, runDesktop: false);
    }

    /// <summary>
    /// 拼接客户端 DLL 的相对路径：Resources\Binaries\{renderFolder}\{dllName}
    /// </summary>
    private static string BuildDllPath(string renderFolder, string dllName)
        => $"Resources{Path.DirectorySeparatorChar}{DotNetBinariesFolder}{Path.DirectorySeparatorChar}{renderFolder}{Path.DirectorySeparatorChar}{dllName}";

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
    /// 显示 GPU 不兼容提示对话框，返回用户选择的按钮索引
    /// </summary>
    private static int? ShowIncompatibleGPUMessage(string[] selections) => AdvancedMessageBox.ShowMessageBoxWithSelection(
            string.Format(
                "客户端检测到您的显卡与 DirectX11 和 OpenGL 版本的 CnCNet 客户端均不兼容。\n\n" +
                "XNA 版本的客户端可能仍可在您的系统上运行，但需要安装\nMicrosoft XNA Framework 4.0 Refresh。\n\n" +
                "您可以从以下链接下载安装程序：\n\n" +
                "{0}\n\n" +
                "或者，您可以重新尝试启动 DirectX11 版本的客户端。\n\n" +
                "对此造成的不便，我们深表歉意。", XnaDownloadLink.ToString()),
            "检测到显卡不兼容",
            selections);

    /// <summary>
    /// 无参数启动时自动选择渲染模式：
    /// Win7/8/10+ 走智能选择逻辑，旧系统直接走 OpenGL
    /// </summary>
    private static void AutoRun()
    {
        if (CurrentOSVersion == OSVersion.LEGACY)
        {
            RunOGL();
            return;
        }

        W7And10Autorun();
    }

    /// <summary>
    /// Win7 及以上系统的智能启动逻辑：
    /// 优先尝试 DX，DX 失败则尝试 OpenGL，两者都失败则回退 XNA 或提示不兼容。
    /// 通过 Client 目录下的 .dxfail / .oglfail 标记文件判断之前是否启动失败
    /// </summary>
    private static void W7And10Autorun()
    {
        string basePath = ToAbsolutePath($"Client{Path.DirectorySeparatorChar}");
        string dxFailFilePath = basePath + ".dxfail";
        string oglFailFilePath = basePath + ".oglfail";

        if (File.Exists(dxFailFilePath))
        {
            // DX 之前失败过
            if (File.Exists(oglFailFilePath))
            {
                // DX 和 OpenGL 都失败过，尝试 XNA 回退
                if (IsXNAFramework4RefreshInstalled())
                {
                    RunXNA();
                    return;
                }

                // XNA 也没装，显示 GPU 不兼容对话框
                int? result = ShowIncompatibleGPUMessage(["打开链接", "启动 XNA 版本", "启动 DirectX11 版本", "退出"]);
                switch (result)
                {
                    case 0:
                        OpenUri(XnaDownloadLink);
                        return;
                    case 1:
                        RunXNA();
                        return;
                    case 2:
                        // 清除失败标记，重新尝试 DX
                        File.Delete(dxFailFilePath);
                        File.Delete(oglFailFilePath);
                        AutoRun();
                        return;
                    default:
                        Environment.Exit(4);
                        return;
                }
            }

            // 仅 DX 失败，走 OpenGL
            RunOGL();
            return;
        }

        // 优先尝试 DX
        RunDX();
    }

    /// <summary>
    /// 使用 .NET 运行时启动指定路径的客户端 DLL
    /// </summary>
    /// <param name="relativePath">客户端 DLL 的相对路径</param>
    /// <param name="run32Bit">是否强制 32 位运行</param>
    /// <param name="runDesktop">是否需要 .NET Desktop Runtime（WPF/WinForms 应用需要）</param>
    private static void StartProcessDotNet(string relativePath, bool run32Bit = false, bool runDesktop = true)
    {
        // 32位系统强制使用 x86 运行时
        if (!Environment.Is64BitOperatingSystem)
            run32Bit = true;

        string architecture = run32Bit ? "x86" : GetMachineArchitecture();

        // 检查 .NET 运行时是否已安装，获取 dotnet host 路径
        string dotnetHost = CheckAndRetrieveDotNetHost(architecture, runDesktop);
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
    /// 检查指定架构的 .NET 运行时是否已安装，返回 dotnet host 的完整路径；
    /// 未安装则提示下载并退出程序
    /// </summary>
    private static string CheckAndRetrieveDotNetHost(string architecture, bool runDesktop)
    {
        // 同时检查 Core Runtime 和 Desktop Runtime（如果需要）
        bool installed = IsDotNetCoreInstalled(architecture)
            && (!runDesktop || IsDotNetDesktopInstalled(architecture));

        if (!installed)
        {
            string missingComponent = runDesktop
                ? $".NET Desktop Runtime 版本 {DotNetMajorVersion}（架构：{architecture}）"
                : $".NET Runtime 版本 {DotNetMajorVersion}（架构：{architecture}）";
            ShowMissingComponent(missingComponent, GetDotNetDownloadLink(architecture, runDesktop));
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
    /// 根据架构和是否需要 Desktop Runtime 返回对应的下载链接
    /// </summary>
    private static Uri GetDotNetDownloadLink(string architecture, bool runDesktop)
    {
        return architecture.ToLowerInvariant() switch
        {
            "x64" => runDesktop ? DotNetX64DesktopRuntimeDownloadLink : DotNetX64RuntimeDownloadLink,
            "x86" => runDesktop ? DotNetX86DesktopRuntimeDownloadLink : DotNetX86RuntimeDownloadLink,
            "arm64" => runDesktop ? DotNetArm64DesktopRuntimeDownloadLink : DotNetArm64RuntimeDownloadLink,
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
    /// 通过注册表检查 XNA Framework 4.0 Refresh 是否已安装
    /// </summary>
    private static bool IsXNAFramework4RefreshInstalled()
    {
        using var localMachine32BitRegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using RegistryKey? xnaKey = localMachine32BitRegistryKey.OpenSubKey("SOFTWARE\\Microsoft\\XNA\\Framework\\v4.0");

        return "1".Equals(xnaKey?.GetValue("Refresh1Installed")?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查指定架构的 .NET Core Runtime 是否已安装
    /// </summary>
    private static bool IsDotNetCoreInstalled(string architecture)
        => IsDotNetInstalled(architecture, "Microsoft.NETCore.App");

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
