using System;
using System.Windows;
using System.Windows.Threading;

namespace SynchronizationCompletionCoordinator.Views
{
    public partial class ConfirmCountdownWindow : Window
    {
        private int _remainingSeconds = AppConfig.ClearConfirmCountdownSeconds;
        private readonly DispatcherTimer _timer;

        public ConfirmCountdownWindow()
        {
            InitializeComponent();
            BtnConfirm.Content = $"确认 ({_remainingSeconds})";

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _remainingSeconds--;
                if (_remainingSeconds <= 0)
                {
                    _timer.Stop();
                    BtnConfirm.IsEnabled = true;
                    BtnConfirm.Content = "确认";
                }
                else
                {
                    BtnConfirm.Content = $"确认 ({_remainingSeconds})";
                }
            };
            _timer.Start();
            Closed += (s, e) => _timer.Stop();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}