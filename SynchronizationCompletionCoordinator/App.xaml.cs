using System;
using System.Windows;

namespace SynchronizationCompletionCoordinator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 捕获 UI 线程异常
            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"程序发生异常：\n{args.Exception.Message}\n\n详细信息：\n{args.Exception}",
                                "程序错误", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            // 捕获后台非 UI 线程异常
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    MessageBox.Show($"程序发生严重异常：\n{ex.Message}\n\n详细信息：\n{ex}",
                                    "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }
    }
}
