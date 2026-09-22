using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using SynchronizationCompletionCoordinator.Models;
using SynchronizationCompletionCoordinator.Services;

namespace SynchronizationCompletionCoordinator.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsModel Settings { get; private set; }
        private readonly double _originalOwnerOpacity = 1.0;

        private static readonly Brush NormalBorderBrush = new SolidColorBrush(Color.FromRgb(171, 173, 179));
        private static readonly Brush ErrorBorderBrush = new SolidColorBrush(Color.FromRgb(244, 67, 54));

        public SettingsWindow(SettingsModel settings)
        {
            InitializeComponent();
            Settings = settings;

            if (Application.Current.MainWindow != null)
            {
                _originalOwnerOpacity = Application.Current.MainWindow.Opacity;
            }

            TxtAttH.Text = settings.AttentionHours.ToString();
            TxtAttM.Text = settings.AttentionMinutes.ToString();
            TxtAttS.Text = settings.AttentionSeconds.ToString();

            TxtWarnH.Text = settings.WarningHours.ToString();
            TxtWarnM.Text = settings.WarningMinutes.ToString();
            TxtWarnS.Text = settings.WarningSeconds.ToString();

            ChkCircleProgress.IsChecked = settings.ShowCircleProgress;
            ChkBorderAlert.IsChecked = settings.ShowBorderAlert;
            ChkBackgroundAlert.IsChecked = settings.ShowBackgroundAlert;

            // 回填窗口半透明值滑动条
            int opacityVal = Math.Clamp(settings.WindowOpacityPercent, 10, 100);
            SliderOpacity.Value = opacityVal;
            TxtOpacityValue.Text = opacityVal.ToString();

            ChkEnableSound.IsChecked = settings.EnableSound;
            TxtAttSound.Text = settings.AttentionSoundPath;
            TxtWarnSound.Text = settings.WarningSoundPath;
            TxtFinishSound.Text = settings.FinishSoundPath;

            // 初始校验输入框路径并更新视觉状态
            ValidatePathVisual(TxtAttSound);
            ValidatePathVisual(TxtWarnSound);
            ValidatePathVisual(TxtFinishSound);

            Closed += SettingsWindow_Closed;
        }

        private void SettingsWindow_Closed(object? sender, EventArgs e)
        {
            // 如果用户点击取消或直接关闭窗口，还原主窗口原先的透明度
            if (DialogResult != true && Owner is MainWindow mw)
            {
                mw.Opacity = _originalOwnerOpacity;
            }
        }

        private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int val = (int)Math.Round(SliderOpacity.Value);
            if (TxtOpacityValue != null)
            {
                TxtOpacityValue.Text = val.ToString();
            }

            // 拖动时若主窗口勾选了窗口半透明，实时改变主窗口透明度
            if (Owner is MainWindow mw && mw.ChkTransparent.IsChecked == true)
            {
                mw.Opacity = val / 100.0;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private bool ValidatePathVisual(TextBox tb)
        {
            string path = tb.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                tb.BorderBrush = NormalBorderBrush;
                tb.ToolTip = null;
                return true;
            }

            string resolved = AudioService.ResolveAudioPath(path);
            if (File.Exists(resolved))
            {
                tb.BorderBrush = NormalBorderBrush;
                tb.ToolTip = resolved;
                return true;
            }
            else
            {
                tb.BorderBrush = ErrorBorderBrush;
                tb.ToolTip = $"文件不存在：{resolved}";
                return false;
            }
        }

        private void SoundPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                ValidatePathVisual(tb);
            }
        }

        private void SoundPath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                ValidatePathVisual(tb);
            }
        }

        private void SoundPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox tb)
            {
                e.Handled = true;
                string text = tb.Text.Trim();
                if (string.IsNullOrEmpty(text)) return;

                if (!ValidatePathVisual(tb))
                {
                    string resolved = AudioService.ResolveAudioPath(text);
                    MessageBox.Show(this, $"未找到音频文件：\n{text}\n\n对应完整路径：\n{resolved}", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                    tb.SelectAll();
                    tb.Focus();
                }
            }
        }

        private void TryPlaySoundWithAlert(string soundTypeTitle, TextBox tb)
        {
            string path = tb.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, $"尚未配置【{soundTypeTitle}】文件路径！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string resolved = AudioService.ResolveAudioPath(path);
            if (!File.Exists(resolved))
            {
                MessageBox.Show(this, $"未找到【{soundTypeTitle}】音频文件，无法试听：\n{path}\n\n完整路径：\n{resolved}", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                tb.SelectAll();
                tb.Focus();
                return;
            }

            AudioService.Play(resolved);
        }

        private void SelectAttentionSound_Click(object sender, RoutedEventArgs e)
        {
            string? file = SelectAudioFile();
            if (file != null) TxtAttSound.Text = file;
        }

        private void SelectWarningSound_Click(object sender, RoutedEventArgs e)
        {
            string? file = SelectAudioFile();
            if (file != null) TxtWarnSound.Text = file;
        }

        private void SelectFinishSound_Click(object sender, RoutedEventArgs e)
        {
            string? file = SelectAudioFile();
            if (file != null) TxtFinishSound.Text = file;
        }

        private void ClearAttSound_Click(object sender, RoutedEventArgs e) => TxtAttSound.Text = string.Empty;
        private void ClearWarnSound_Click(object sender, RoutedEventArgs e) => TxtWarnSound.Text = string.Empty;
        private void ClearFinishSound_Click(object sender, RoutedEventArgs e) => TxtFinishSound.Text = string.Empty;

        private void TestAttSound_Click(object sender, RoutedEventArgs e) => TryPlaySoundWithAlert("注意状态提示音", TxtAttSound);
        private void TestWarnSound_Click(object sender, RoutedEventArgs e) => TryPlaySoundWithAlert("警告状态提示音", TxtWarnSound);
        private void TestFinishSound_Click(object sender, RoutedEventArgs e) => TryPlaySoundWithAlert("计时结束提示音", TxtFinishSound);

        private string? SelectAudioFile()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "音频文件 (*.wav;*.mp3;*.m4a;*.wma)|*.wav;*.mp3;*.m4a;*.wma|所有文件 (*.*)|*.*"
            };
            return dlg.ShowDialog(this) == true ? dlg.FileName : null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(TxtAttH.Text, out int attH);
            int.TryParse(TxtAttM.Text, out int attM);
            int.TryParse(TxtAttS.Text, out int attS);

            int.TryParse(TxtWarnH.Text, out int warnH);
            int.TryParse(TxtWarnM.Text, out int warnM);
            int.TryParse(TxtWarnS.Text, out int warnS);

            int totalAtt = attH * 3600 + attM * 60 + attS;
            int totalWarn = warnH * 3600 + warnM * 60 + warnS;

            if (totalWarn >= totalAtt)
            {
                MessageBox.Show(this, "警告状态时机必须小于注意状态时机！", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 保存前验证提示音文件是否存在，存在无效路径时阻止保存
            if (!ValidatePathVisual(TxtAttSound))
            {
                MessageBox.Show(this, $"注意状态提示音文件不存在：\n{TxtAttSound.Text.Trim()}", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAttSound.SelectAll();
                TxtAttSound.Focus();
                return;
            }

            if (!ValidatePathVisual(TxtWarnSound))
            {
                MessageBox.Show(this, $"警告状态提示音文件不存在：\n{TxtWarnSound.Text.Trim()}", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtWarnSound.SelectAll();
                TxtWarnSound.Focus();
                return;
            }

            if (!ValidatePathVisual(TxtFinishSound))
            {
                MessageBox.Show(this, $"计时结束提示音文件不存在：\n{TxtFinishSound.Text.Trim()}", "文件不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFinishSound.SelectAll();
                TxtFinishSound.Focus();
                return;
            }

            int opacityVal = (int)Math.Round(SliderOpacity.Value);

            Settings.AttentionHours = attH;
            Settings.AttentionMinutes = attM;
            Settings.AttentionSeconds = attS;

            Settings.WarningHours = warnH;
            Settings.WarningMinutes = warnM;
            Settings.WarningSeconds = warnS;

            Settings.ShowCircleProgress = ChkCircleProgress.IsChecked == true;
            Settings.ShowBorderAlert = ChkBorderAlert.IsChecked == true;
            Settings.ShowBackgroundAlert = ChkBackgroundAlert.IsChecked == true;
            Settings.WindowOpacityPercent = opacityVal;

            Settings.EnableSound = ChkEnableSound.IsChecked == true;
            Settings.AttentionSoundPath = TxtAttSound.Text.Trim();
            Settings.WarningSoundPath = TxtWarnSound.Text.Trim();
            Settings.FinishSoundPath = TxtFinishSound.Text.Trim();

            DialogResult = true;
        }
    }
}