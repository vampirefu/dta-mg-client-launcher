namespace CnCNet.LauncherStub;

using System.Collections.ObjectModel;
using System.Windows;

/// <summary>
/// 自定义消息对话框，支持动态按钮和选择结果返回。
/// 包含对话框视图和辅助方法
/// </summary>
public partial class AdvancedMessageBox : Window
{
    public AdvancedMessageBox() => InitializeComponent();

    /// <summary>
    /// 用户选择的结果索引，null 表示未选择（如直接关闭窗口）
    /// </summary>
    public object? Result { get; set; }

    #region 静态辅助方法

    /// <summary>
    /// 显示带多选按钮的消息对话框，返回用户点击的按钮索引
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <param name="title">对话框标题</param>
    /// <param name="selections">按钮文本数组</param>
    /// <returns>用户点击的按钮索引，null 表示未选择</returns>
    public static int? ShowMessageBoxWithSelection(string message, string title, string[] selections)
    {
        var msgbox = new AdvancedMessageBox();
        var model = (AdvancedMessageBoxViewModel)msgbox.DataContext;
        model.Title = title;
        model.Message = message;

        var commands = new ObservableCollection<CommandViewModel>();
        for (int i = 0; i < selections.Length; i++)
        {
            // 必须捕获循环变量，避免闭包捕获导致的 bug
            int iCaptured = i;
            commands.Add(new CommandViewModel()
            {
                Text = selections[i],
                Command = new RelayCommand(_ =>
                {
                    msgbox.Result = iCaptured;
                    msgbox.Close();
                }),
            });
        }

        model.Commands = commands;
        msgbox.ShowDialog();
        return msgbox.Result as int?;
    }

    /// <summary>
    /// 显示带单个"确定"按钮的消息对话框
    /// </summary>
    public static void ShowOkMessageBox(string message, string title, string okText = "确定")
        => ShowMessageBoxWithSelection(message, title, new[] { okText });

    /// <summary>
    /// 显示带"是/否"两个按钮的消息对话框，返回是否点击了第一个按钮
    /// </summary>
    public static bool ShowYesNoMessageBox(string message, string title, string yesText = "是", string noText = "否")
    {
        int? result = ShowMessageBoxWithSelection(message, title, new[] { yesText, noText });
        return result == 0;
    }

    #endregion
}
