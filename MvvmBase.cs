namespace CnCNet.LauncherStub;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

/// <summary>
/// MVVM 基础设施：属性变更通知基类、命令绑定、ViewModel
/// </summary>

#region 属性变更通知基类

/// <summary>
/// 实现 INotifyPropertyChanged 的抽象基类，提供属性变更通知能力
/// </summary>
public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 通知属性变更，通过 CallerMemberName 自动获取调用方的属性名
    /// </summary>
    internal void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

#endregion

#region 命令绑定

/// <summary>
/// 通用命令实现，将 ICommand 委托给指定的 Action
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object> execute;

    /// <param name="execute">命令执行时的回调</param>
    public RelayCommand(Action<object> execute) => this.execute = execute;

    public event EventHandler CanExecuteChanged;

    /// <summary>始终允许执行</summary>
    public bool CanExecute(object parameter) => true;

    /// <summary>执行绑定的回调</summary>
    public void Execute(object parameter) => execute(parameter);
}

#endregion

#region 命令 ViewModel

/// <summary>
/// 表示对话框中的一个按钮命令，包含显示文本和关联的 ICommand
/// </summary>
public class CommandViewModel : NotifyPropertyChangedBase
{
    private string? text;

    /// <summary>按钮显示文本</summary>
    public string Text
    {
        get => text;
        set { text = value; NotifyPropertyChanged(); }
    }

    private ICommand? command;

    /// <summary>按钮关联的命令</summary>
    public ICommand Command
    {
        get => command;
        set { command = value; NotifyPropertyChanged(); }
    }
}

#endregion

#region AdvancedMessageBox ViewModel

/// <summary>
/// AdvancedMessageBox 的 ViewModel，管理标题、消息内容和命令列表
/// </summary>
public class AdvancedMessageBoxViewModel : NotifyPropertyChangedBase
{
    private ObservableCollection<CommandViewModel>? commands;

    /// <summary>对话框底部显示的按钮命令列表</summary>
    public ObservableCollection<CommandViewModel> Commands
    {
        get => commands;
        set { commands = value; NotifyPropertyChanged(); }
    }

    private string? title;

    /// <summary>对话框标题</summary>
    public string Title
    {
        get => title;
        set { title = value; NotifyPropertyChanged(); }
    }

    private string? message;

    /// <summary>对话框消息内容</summary>
    public string Message
    {
        get => message;
        set { message = value; NotifyPropertyChanged(); }
    }
}

#endregion
