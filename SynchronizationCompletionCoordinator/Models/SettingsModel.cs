using System.Text.Json.Serialization;

namespace SynchronizationCompletionCoordinator.Models
{
    public class SettingsModel
    {
        // 团队时间模式：StartTime / EndTime
        public string TeamTimeMode { get; set; } = AppConfig.ModeStartTime;

        // 窗口置顶与锁定尺寸
        public bool WindowTopMost { get; set; } = false;
        public bool LockWindowResize { get; set; } = false;

        // 注意状态提前量（时、分、秒）
        public int AttentionHours { get; set; } = 0;
        public int AttentionMinutes { get; set; } = 1;
        public int AttentionSeconds { get; set; } = 0;

        // 警告状态提前量（时、分、秒）
        public int WarningHours { get; set; } = 0;
        public int WarningMinutes { get; set; } = 0;
        public int WarningSeconds { get; set; } = 30;

        // 可视化设置
        public bool ShowCircleProgress { get; set; } = true;
        public bool ShowBorderAlert { get; set; } = true;
        public bool ShowBackgroundAlert { get; set; } = false;

        // 窗口半透明设置
        public bool WindowTransparent { get; set; } = false;
        public int WindowOpacityPercent { get; set; } = AppConfig.DefaultWindowOpacityPercent;

        // 提示音总开关与路径
        public bool EnableSound { get; set; } = true;
        public string AttentionSoundPath { get; set; } = string.Empty;
        public string WarningSoundPath { get; set; } = string.Empty;
        public string FinishSoundPath { get; set; } = string.Empty;

        // 换算总秒数便利方法（忽略序列化，避免写入配置文件）
        [JsonIgnore]
        public int TotalAttentionSeconds => AttentionHours * 3600 + AttentionMinutes * 60 + AttentionSeconds;

        [JsonIgnore]
        public int TotalWarningSeconds => WarningHours * 3600 + WarningMinutes * 60 + WarningSeconds;
    }

    public class UnitItemConfig
    {
        public string Name { get; set; } = string.Empty;
        public int Days { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
    }

    public class AppConfigFile
    {
        public SettingsModel Settings { get; set; } = new SettingsModel();
        public System.Collections.Generic.List<UnitItemConfig> Units { get; set; } = new();
    }
}