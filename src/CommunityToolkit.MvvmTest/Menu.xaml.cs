using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp
{
    /// <summary>
    /// 提供自定义控件示例菜单的主窗口类
    /// </summary>
    public partial class Menu : Window
    {
        /// <summary>
        /// 初始化 Menu 类的新实例
        /// </summary>
        public Menu()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理按钮点击事件，根据按钮内容创建并显示相应的窗口
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            // 获取触发事件的按钮
            Button cmd = (Button)e.OriginalSource;

            // 获取当前程序集和命名空间信息，用于创建指定类型的窗口实例
            Type type = this.GetType();
            Assembly assembly = type.Assembly;
            Window win = (Window)assembly.CreateInstance(type.Namespace + "." + cmd.Content);
            // 显示新创建的窗口
            win.Show();
        }
    }
}