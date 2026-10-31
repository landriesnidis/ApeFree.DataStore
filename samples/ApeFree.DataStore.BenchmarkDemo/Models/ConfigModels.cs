using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.DataStore.BenchmarkDemo.Models
{
    /// <summary>
    /// 视窗布局配置
    /// </summary>
    [Serializable]
    public class WindowStateConfig
    {
        public int Width { get; set; } = 1280;
        public int Height { get; set; } = 800;
        public int PosX { get; set; } = 100;
        public int PosY { get; set; } = 100;
        public bool IsMaximized { get; set; } = false;
    }

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    [Serializable]
    public class UserSettingsConfig
    {
        public string Theme { get; set; } = "Dark";
        public string Language { get; set; } = "zh-CN";
        public bool AutoSave { get; set; } = true;
        public int RefreshRateMs { get; set; } = 60;
        public bool EnableHardwareAcceleration { get; set; } = true;
    }

    /// <summary>
    /// 应用程序高频读写配置实体
    /// </summary>
    [Serializable]
    public class AppConfig
    {
        public int Counter { get; set; }
        public string AppName { get; set; } = "ApeFree.DataStore.Benchmark";
        public string Version { get; set; } = "3.0.0";
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public WindowStateConfig WindowState { get; set; } = new WindowStateConfig();
        public UserSettingsConfig UserSettings { get; set; } = new UserSettingsConfig();
        public List<string> RecentFiles { get; set; } = new List<string>();
        public Dictionary<string, string> CustomPreferences { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 创建样本配置数据
        /// </summary>
        public static AppConfig CreateSampleConfig(bool complex = false)
        {
            var config = new AppConfig
            {
                Counter = 0,
                LastUpdated = DateTime.Now
            };

            for (int i = 0; i < (complex ? 50 : 5); i++)
            {
                config.RecentFiles.Add($@"C:\Projects\ApeFree\Workspace\File_{i:D4}.dat");
                config.CustomPreferences[$"Key_Setting_{i}"] = $"CustomValue_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            return config;
        }

        /// <summary>
        /// 浅克隆以避免并发写干扰
        /// </summary>
        public AppConfig Clone()
        {
            return new AppConfig
            {
                Counter = this.Counter,
                AppName = this.AppName,
                Version = this.Version,
                LastUpdated = this.LastUpdated,
                WindowState = new WindowStateConfig
                {
                    Width = this.WindowState.Width,
                    Height = this.WindowState.Height,
                    PosX = this.WindowState.PosX,
                    PosY = this.WindowState.PosY,
                    IsMaximized = this.WindowState.IsMaximized
                },
                UserSettings = new UserSettingsConfig
                {
                    Theme = this.UserSettings.Theme,
                    Language = this.UserSettings.Language,
                    AutoSave = this.UserSettings.AutoSave,
                    RefreshRateMs = this.UserSettings.RefreshRateMs,
                    EnableHardwareAcceleration = this.UserSettings.EnableHardwareAcceleration
                },
                RecentFiles = new List<string>(this.RecentFiles),
                CustomPreferences = new Dictionary<string, string>(this.CustomPreferences)
            };
        }
    }
}
