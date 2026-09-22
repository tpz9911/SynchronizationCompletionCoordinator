using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SynchronizationCompletionCoordinator.Models
{
    public enum UnitExecutionState
    {
        Waiting,    // 正常等待中
        Attention,  // 进入注意状态（黄色）
        Warning,    // 进入警告状态（红色）
        Executing,  // 已开始执行（绿色）
        Finished    // 团队完成（灰色）
    }

    public class UnitItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private TimeSpan _duration;
        private DateTime _startTime;
        private TimeSpan _startOffset;
        private string _countdownDisplay = string.Empty;
        private Brush _countdownForeground = AppConfig.NormalCountdownBrush;
        private double _circleProgressRatio = 1.0;
        private UnitExecutionState _state = UnitExecutionState.Waiting;

        // 提示音单次播放标记
        public bool PlayedAttentionSound { get; set; }
        public bool PlayedWarningSound { get; set; }
        public bool PlayedFinishedSound { get; set; }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationDisplay));
            }
        }

        public string DurationDisplay => FormatTimeSpan(Duration);

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartTimeDisplay));
            }
        }

        public string StartTimeDisplay => StartTime == DateTime.MinValue ? string.Empty : StartTime.ToString(AppConfig.DateTimeFormat);

        public TimeSpan StartOffset
        {
            get => _startOffset;
            set
            {
                _startOffset = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartOffsetDisplay));
            }
        }

        public string StartOffsetDisplay => FormatTimeSpan(StartOffset);

        public string CountdownDisplay
        {
            get => _countdownDisplay;
            set { _countdownDisplay = value; OnPropertyChanged(); }
        }

        public Brush CountdownForeground
        {
            get => _countdownForeground;
            set { _countdownForeground = value; OnPropertyChanged(); }
        }

        public double CircleProgressRatio
        {
            get => _circleProgressRatio;
            set { _circleProgressRatio = Math.Clamp(value, 0.0, 1.0); OnPropertyChanged(); }
        }

        public UnitExecutionState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); }
        }

        public static string FormatTimeSpan(TimeSpan ts, bool isNegative = false)
        {
            int totalDays = (int)Math.Abs(ts.TotalDays);
            int hours = Math.Abs(ts.Hours);
            int minutes = Math.Abs(ts.Minutes);
            int seconds = Math.Abs(ts.Seconds);

            string dayPart = totalDays == 0 ? "  " : $"{totalDays:D2}";
            string sign = isNegative ? "-" : "";
            return $"{sign}{dayPart} {hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}