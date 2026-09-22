using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using SynchronizationCompletionCoordinator.Models;
using SynchronizationCompletionCoordinator.Services;
using SynchronizationCompletionCoordinator.Views;

namespace SynchronizationCompletionCoordinator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private AppConfigFile _configFile = new();
        private readonly ObservableCollection<UnitItem> _units = new();
        private readonly DispatcherTimer _ticker = new();

        private DateTime? _configuredAtTime = null;
        private DateTime? _teamStartTime = null;
        private DateTime? _teamEndTime = null;

        private int _blinkTickCounter = 0;
        // 默认为 true：在 InitializeComponent() 和 LoadSettings() 完成前，阻断一切事件触发的保存
        private bool _isLoadingConfig = true;

        // Win32 常量定义
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int ResizeBorderThickness = 6;

        public Visibility CircleProgressVisibility => _configFile.Settings.ShowCircleProgress ? Visibility.Visible : Visibility.Collapsed;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            GridUnits.ItemsSource = _units;

            DpTeamDate.DisplayDateStart = DateTime.Today;
            DpTeamDate.SelectedDate = DateTime.Today;

            LoadSettings();

            _ticker.Interval = TimeSpan.FromMilliseconds(100);
            _ticker.Tick += OnTick;
            _ticker.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                // 最大化或已勾选锁定尺寸时不响应边框调整
                if (WindowState == WindowState.Maximized || ChkLockWindowResize?.IsChecked == true)
                    return IntPtr.Zero;

                int x = (short)(lParam.ToInt64() & 0xFFFF);
                int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

                Point clientPoint = PointFromScreen(new Point(x, y));

                double width = ActualWidth;
                double height = ActualHeight;

                bool left = clientPoint.X <= ResizeBorderThickness;
                bool right = clientPoint.X >= width - ResizeBorderThickness;
                bool top = clientPoint.Y <= ResizeBorderThickness;
                bool bottom = clientPoint.Y >= height - ResizeBorderThickness;

                if (top && left) { handled = true; return (IntPtr)HTTOPLEFT; }
                if (top && right) { handled = true; return (IntPtr)HTTOPRIGHT; }
                if (bottom && left) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
                if (bottom && right) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
                if (left) { handled = true; return (IntPtr)HTLEFT; }
                if (right) { handled = true; return (IntPtr)HTRIGHT; }
                if (top) { handled = true; return (IntPtr)HTTOP; }
                if (bottom) { handled = true; return (IntPtr)HTBOTTOM; }
            }

            return IntPtr.Zero;
        }

        private void LoadSettings()
        {
            _isLoadingConfig = true;
            try
            {
                _configFile = ConfigService.LoadConfig();

                if (_configFile.Settings.TeamTimeMode == AppConfig.ModeEndTime)
                    RbEndTime.IsChecked = true;
                else
                    RbStartTime.IsChecked = true;

                // 回显窗口置顶
                ChkTopMost.IsChecked = _configFile.Settings.WindowTopMost;
                Topmost = _configFile.Settings.WindowTopMost;

                // 回显锁定窗口尺寸
                ChkLockWindowResize.IsChecked = _configFile.Settings.LockWindowResize;

                // 回显半透明勾选设置
                ChkTransparent.IsChecked = _configFile.Settings.WindowTransparent;
                UpdateWindowOpacity();

                _units.Clear();
                foreach (var u in _configFile.Units)
                {
                    var duration = new TimeSpan(u.Days, u.Hours, u.Minutes, u.Seconds);
                    _units.Add(new UnitItem { Name = u.Name, Duration = duration });
                }
                OnPropertyChanged(nameof(CircleProgressVisibility));
            }
            finally
            {
                _isLoadingConfig = false;
            }
        }

        private void SaveConfig()
        {
            if (_isLoadingConfig) return;

            _configFile.Units = _units.Select(u => new UnitItemConfig
            {
                Name = u.Name,
                Days = u.Duration.Days,
                Hours = u.Duration.Hours,
                Minutes = u.Duration.Minutes,
                Seconds = u.Duration.Seconds
            }).ToList();

            _configFile.Settings.TeamTimeMode = RbEndTime?.IsChecked == true ? AppConfig.ModeEndTime : AppConfig.ModeStartTime;
            _configFile.Settings.WindowTransparent = ChkTransparent?.IsChecked == true;
            _configFile.Settings.WindowTopMost = ChkTopMost?.IsChecked == true;
            _configFile.Settings.LockWindowResize = ChkLockWindowResize?.IsChecked == true;
            ConfigService.SaveConfig(_configFile);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void ChkTopMost_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingConfig) return;
            Topmost = ChkTopMost?.IsChecked == true;
            SaveConfig();
        }

        private void ChkLockWindowResize_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingConfig) return;
            SaveConfig();
        }

        private void ChkTransparent_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoadingConfig) return;
            UpdateWindowOpacity();
            SaveConfig();
        }

        private void UpdateWindowOpacity()
        {
            if (ChkTransparent?.IsChecked == true)
            {
                int percent = _configFile.Settings.WindowOpacityPercent;
                if (percent < AppConfig.MinWindowOpacityPercent || percent > 100)
                {
                    percent = AppConfig.DefaultWindowOpacityPercent;
                }
                Opacity = percent / 100.0;
            }
            else
            {
                Opacity = 1.0;
            }
        }

        // 按住主窗口空白区域拖动窗口
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && WindowState == WindowState.Normal)
            {
                DragMove();
            }
        }

        // 右键菜单打开前动态控制最大化与还原状态
        private void WindowContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            bool isMaximized = WindowState == WindowState.Maximized;
            MenuMaximize.Visibility = isMaximized ? Visibility.Collapsed : Visibility.Visible;
            MenuRestore.Visibility = isMaximized ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MenuMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
        }

        private void MenuRestore_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
        }

        private void MenuMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开链接失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Mode_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoadingConfig) return;
            RecalculateSchedule();
            SaveConfig();
        }

        private void TeamTimeInput_Changed(object sender, RoutedEventArgs e)
        {
        }

        private void BtnApplyTeamTime_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetInputDateTime(out DateTime userDateTime, silent: false))
                return;

            if (userDateTime <= DateTime.Now)
            {
                MessageBox.Show(this, "设置的团队时间不能为过去时间！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _configuredAtTime = DateTime.Now;

            foreach (var u in _units)
            {
                u.PlayedAttentionSound = false;
                u.PlayedWarningSound = false;
                u.PlayedFinishedSound = false;
            }

            RecalculateSchedule();
        }

        private bool TryGetInputDateTime(out DateTime result, bool silent)
        {
            result = DateTime.MinValue;
            DateTime baseDate = DpTeamDate.SelectedDate ?? DateTime.Today;

            int.TryParse(TxtHour.Text, out int h);
            int.TryParse(TxtMinute.Text, out int m);
            int.TryParse(TxtSecond.Text, out int s);

            if (h > AppConfig.MaxHours || m > AppConfig.MaxMinutes || s > AppConfig.MaxSeconds)
            {
                if (!silent)
                {
                    MessageBox.Show(this, "时间超出有效范围 (时0-23, 分0-59, 秒0-59)！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return false;
            }

            result = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, h, m, s);
            return true;
        }

        private void RecalculateSchedule()
        {
            if (_configuredAtTime == null || _units.Count == 0) return;
            if (!TryGetInputDateTime(out DateTime specifiedTime, silent: true)) return;

            TimeSpan maxDuration = _units.Max(u => u.Duration);

            if (RbStartTime.IsChecked == true)
            {
                _teamStartTime = specifiedTime;
                _teamEndTime = specifiedTime + maxDuration;
            }
            else
            {
                _teamEndTime = specifiedTime;
                _teamStartTime = specifiedTime - maxDuration;
            }

            TxtEarliestStartTime.Text = _teamStartTime.Value.ToString(AppConfig.DateTimeFormat);
            TxtTeamEndTime.Text = _teamEndTime.Value.ToString(AppConfig.DateTimeFormat);

            foreach (var unit in _units)
            {
                unit.StartTime = _teamEndTime.Value - unit.Duration;
                unit.StartOffset = unit.StartTime - _teamStartTime.Value;
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _blinkTickCounter++;
            DateTime now = DateTime.Now;

            if (_teamEndTime == null || _configuredAtTime == null)
            {
                TxtProgressDisplay.Text = "等待设定团队时间";
                return;
            }

            // 1. 团队进度条刷新 (绿色)
            TimeSpan totalSpan = _teamEndTime.Value - _configuredAtTime.Value;
            TimeSpan leftSpan = _teamEndTime.Value - now;

            if (leftSpan <= TimeSpan.Zero)
            {
                TeamProgressBar.Value = 0;
                TeamProgressBar.Foreground = Brushes.Gray;
                TxtProgressDisplay.Text = "团队完成";
            }
            else
            {
                double ratio = totalSpan.TotalSeconds > 0 ? (leftSpan.TotalSeconds / totalSpan.TotalSeconds) * 100.0 : 0;
                TeamProgressBar.Value = Math.Clamp(ratio, 0, 100);
                TeamProgressBar.Foreground = AppConfig.TeamProgressBrush;
                TxtProgressDisplay.Text = $"团队完成倒计时：{UnitItem.FormatTimeSpan(leftSpan)}";
            }

            // 2. 表格各单位倒计时与状态计算
            bool is2HzOn = (_blinkTickCounter % 5) < 3;
            bool is5HzOn = (_blinkTickCounter % 2) == 0;

            int attSec = _configFile.Settings.TotalAttentionSeconds;
            int warnSec = _configFile.Settings.TotalWarningSeconds;
            bool soundEnabled = _configFile.Settings.EnableSound;

            foreach (var unit in _units)
            {
                if (now >= _teamEndTime.Value)
                {
                    unit.State = UnitExecutionState.Finished;
                    unit.CountdownDisplay = AppConfig.CompletedStatusText;
                    unit.CountdownForeground = AppConfig.ExecutingCountdownBrush;
                    unit.CircleProgressRatio = 0.0;

                    if (!unit.PlayedFinishedSound)
                    {
                        unit.PlayedFinishedSound = true;
                        if (soundEnabled) AudioService.Play(_configFile.Settings.FinishSoundPath);
                    }
                    continue;
                }

                TimeSpan diff = unit.StartTime - now;

                if (diff <= TimeSpan.Zero)
                {
                    // 已经开始执行：文字变灰以降低显眼度
                    unit.State = UnitExecutionState.Executing;
                    TimeSpan executed = now - unit.StartTime;
                    unit.CountdownDisplay = UnitItem.FormatTimeSpan(executed, isNegative: true);
                    unit.CountdownForeground = AppConfig.ExecutingCountdownBrush;
                    unit.CircleProgressRatio = 0.0;
                }
                else
                {
                    // 未开始等待中：正常深黑色文字
                    double totalWait = (unit.StartTime - _configuredAtTime.Value).TotalSeconds;
                    unit.CircleProgressRatio = totalWait > 0 ? diff.TotalSeconds / totalWait : 0.0;
                    unit.CountdownDisplay = UnitItem.FormatTimeSpan(diff, isNegative: false);
                    unit.CountdownForeground = AppConfig.NormalCountdownBrush;

                    if (diff.TotalSeconds <= warnSec)
                    {
                        unit.State = UnitExecutionState.Warning;
                        if (!unit.PlayedWarningSound)
                        {
                            unit.PlayedWarningSound = true;
                            if (soundEnabled) AudioService.Play(_configFile.Settings.WarningSoundPath);
                        }
                    }
                    else if (diff.TotalSeconds <= attSec)
                    {
                        unit.State = UnitExecutionState.Attention;
                        if (!unit.PlayedAttentionSound)
                        {
                            unit.PlayedAttentionSound = true;
                            if (soundEnabled) AudioService.Play(_configFile.Settings.AttentionSoundPath);
                        }
                    }
                    else
                    {
                        unit.State = UnitExecutionState.Waiting;
                    }
                }
            }

            // 3. 动态应用行边框闪烁与背景色
            ApplyVisualAlerts(is2HzOn, is5HzOn);
        }

        private void ApplyVisualAlerts(bool is2HzOn, bool is5HzOn)
        {
            bool showBorder = _configFile.Settings.ShowBorderAlert;
            bool showBg = _configFile.Settings.ShowBackgroundAlert;

            foreach (var item in _units)
            {
                if (GridUnits.ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow row)
                    continue;

                if (row.Template.FindName("InnerStateBorder", row) is not Border innerBorder)
                    continue;

                Brush stateBrush = Brushes.Transparent;
                bool isBlinkVisible = true;

                switch (item.State)
                {
                    case UnitExecutionState.Attention:
                        stateBrush = new SolidColorBrush(Color.FromArgb((byte)(AppConfig.AlertBorderOpacity * 255), 255, 193, 7));
                        isBlinkVisible = is2HzOn;
                        break;
                    case UnitExecutionState.Warning:
                        stateBrush = new SolidColorBrush(Color.FromArgb((byte)(AppConfig.AlertBorderOpacity * 255), 244, 67, 54));
                        isBlinkVisible = is5HzOn;
                        break;
                    case UnitExecutionState.Executing:
                        stateBrush = new SolidColorBrush(Color.FromArgb((byte)(AppConfig.AlertBorderOpacity * 255), 76, 175, 80));
                        break;
                    case UnitExecutionState.Finished:
                        stateBrush = new SolidColorBrush(Color.FromArgb((byte)(AppConfig.AlertBorderOpacity * 255), 158, 158, 158));
                        break;
                    default:
                        stateBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                        break;
                }

                innerBorder.BorderBrush = showBorder ? (isBlinkVisible ? stateBrush : Brushes.Transparent) : new SolidColorBrush(Color.FromRgb(220, 220, 220));
                innerBorder.Background = (showBg && item.State != UnitExecutionState.Waiting) ? stateBrush : Brushes.White;
            }
        }

        // 点击空白区域取消选择
        private void GridUnits_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep != GridUnits)
            {
                if (dep is DataGridRow)
                    return;
                dep = VisualTreeHelper.GetParent(dep);
            }

            GridUnits.SelectedItem = null;
        }

        // --- 按钮事件 ---

        private void BtnAddUnit_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UnitEditWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var cfg = dlg.ResultConfig;
                _units.Add(new UnitItem
                {
                    Name = cfg.Name,
                    Duration = new TimeSpan(cfg.Days, cfg.Hours, cfg.Minutes, cfg.Seconds)
                });
                RecalculateSchedule();
                SaveConfig();
            }
        }

        private void BtnDeleteUnit_Click(object sender, RoutedEventArgs e)
        {
            if (GridUnits.SelectedItem is not UnitItem selected)
            {
                MessageBox.Show(this, "请先选择单位！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = MessageBox.Show(this, $"您正在删除单位【{selected.Name}】，是否确认？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _units.Remove(selected);
                RecalculateSchedule();
                SaveConfig();
            }
        }

        private void BtnClearUnits_Click(object sender, RoutedEventArgs e)
        {
            var confirmDlg = new ConfirmCountdownWindow { Owner = this };
            if (confirmDlg.ShowDialog() == true)
            {
                _units.Clear();

                _configuredAtTime = null;
                _teamStartTime = null;
                _teamEndTime = null;

                TxtEarliestStartTime.Text = "--";
                TxtTeamEndTime.Text = "--";
                TeamProgressBar.Value = 100;
                TeamProgressBar.Foreground = AppConfig.TeamProgressBrush;
                TxtProgressDisplay.Text = "等待设定团队时间";

                SaveConfig();
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsWindow(_configFile.Settings) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _configFile.Settings = dlg.Settings;
                OnPropertyChanged(nameof(CircleProgressVisibility));
                UpdateWindowOpacity();
                SaveConfig();
            }
        }

        private void GridUnits_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GridUnits.SelectedItem is not UnitItem item) return;

            e.Handled = true;

            var existingConfig = new UnitItemConfig
            {
                Name = item.Name,
                Days = item.Duration.Days,
                Hours = item.Duration.Hours,
                Minutes = item.Duration.Minutes,
                Seconds = item.Duration.Seconds
            };

            var dlg = new UnitEditWindow(existingConfig) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                item.Name = dlg.ResultConfig.Name;
                item.Duration = new TimeSpan(dlg.ResultConfig.Days, dlg.ResultConfig.Hours, dlg.ResultConfig.Minutes, dlg.ResultConfig.Seconds);
                RecalculateSchedule();
                SaveConfig();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}