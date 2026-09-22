using System;
using System.Windows;
using System.Windows.Media;

namespace SynchronizationCompletionCoordinator
{
    public static class AppConfig
    {
        // 配置文件名称
        public const string ConfigFileName = "scc_config.json";

        // 清空列表二次确认倒计时（秒）
        public const int ClearConfirmCountdownSeconds = 5;

        // 闪烁频率定义（Hz）
        public const double AttentionBlinkFrequencyHz = 2.0; // 注意状态 2Hz
        public const double WarningBlinkFrequencyHz = 5.0;   // 警告状态 5Hz

        // 边框不透明度
        public const double AlertBorderOpacity = 0.90;

        // 粗细数值定义（像素）
        public const double SelectionBorderThicknessValue = 2.5; // 选择框粗细数值
        public const double AlertBorderThicknessValue = 2.0;     // 状态提示边框粗细数值

        // 窗口半透明百分比设定
        public const int MinWindowOpacityPercent = 10;           // 最小值 10%
        public const int MaxWindowOpacityPercent = 99;           // 最大值 99%
        public const int DefaultWindowOpacityPercent = 80;       // 默认值 80%

        // WPF Thickness 属性
        public static readonly Thickness SelectionBorderThickness = new Thickness(SelectionBorderThicknessValue);
        public static readonly Thickness AlertBorderThickness = new Thickness(AlertBorderThicknessValue);

        // 进度条与圆环颜色
        public static readonly Color TeamProgressColor = Color.FromRgb(46, 125, 50);
        public static readonly Color CircleProgressColor = Color.FromRgb(46, 125, 50);

        // 固化画刷
        public static readonly Brush TeamProgressBrush = CreateFrozenBrush(TeamProgressColor);
        public static readonly Brush CircleProgressBrush = CreateFrozenBrush(CircleProgressColor);
        public static readonly Brush ExecutingCountdownBrush = CreateFrozenBrush(Color.FromRgb(128, 128, 128));
        public static readonly Brush NormalCountdownBrush = CreateFrozenBrush(Color.FromRgb(33, 33, 33));

        // 默认状态时机提前量（秒）
        public const int DefaultAttentionThresholdSeconds = 60;
        public const int DefaultWarningThresholdSeconds = 30;

        // 界面与倒计时格式化
        public const string DateFormat = "yyyy-MM-dd";
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        public const string TimeFormat = "HH:mm:ss";
        public const string CompletedStatusText = "已完成";

        // 输入限制
        public const int MaxDays = 99;
        public const int MaxHours = 23;
        public const int MaxMinutes = 59;
        public const int MaxSeconds = 59;

        // 提示与文本
        public const string AppTitle = "同步完成协调器";
        public const string ModeStartTime = "开始时间";
        public const string ModeEndTime = "完成时间";

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}