using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using SynchronizationCompletionCoordinator.Models;

namespace SynchronizationCompletionCoordinator.Views
{
    public partial class UnitEditWindow : Window
    {
        public UnitItemConfig ResultConfig { get; private set; } = new();

        public UnitEditWindow(UnitItemConfig? existing = null)
        {
            InitializeComponent();
            if (existing != null)
            {
                TxtName.Text = existing.Name;
                TxtDays.Text = existing.Days.ToString();
                TxtHours.Text = existing.Hours.ToString();
                TxtMinutes.Text = existing.Minutes.ToString();
                TxtSeconds.Text = existing.Seconds.ToString();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "请输入单位名称！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(TxtDays.Text, out int days);
            int.TryParse(TxtHours.Text, out int hours);
            int.TryParse(TxtMinutes.Text, out int minutes);
            int.TryParse(TxtSeconds.Text, out int seconds);

            if (days > AppConfig.MaxDays) { ShowRangeError("天数", 0, AppConfig.MaxDays); return; }
            if (hours > AppConfig.MaxHours) { ShowRangeError("小时", 0, AppConfig.MaxHours); return; }
            if (minutes > AppConfig.MaxMinutes) { ShowRangeError("分钟", 0, AppConfig.MaxMinutes); return; }
            if (seconds > AppConfig.MaxSeconds) { ShowRangeError("秒钟", 0, AppConfig.MaxSeconds); return; }

            if (days == 0 && hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show(this, "执行时间不能为0！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ResultConfig = new UnitItemConfig
            {
                Name = name,
                Days = days,
                Hours = hours,
                Minutes = minutes,
                Seconds = seconds
            };

            DialogResult = true;
        }

        private void ShowRangeError(string field, int min, int max)
        {
            MessageBox.Show(this, $"{field} 范围必须在 {min}~{max} 之间！", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}