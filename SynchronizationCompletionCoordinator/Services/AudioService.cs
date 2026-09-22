using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace SynchronizationCompletionCoordinator.Services
{
    public static class AudioService
    {
        // 保持对活跃 MediaPlayer 的强引用，防止被 .NET GC 垃圾回收器中途回收导致声音戛然而止
        private static readonly List<MediaPlayer> ActivePlayers = new();
        private static readonly object LockObj = new();

        public static string ResolveAudioPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return string.Empty;

            if (Path.IsPathRooted(filePath))
                return filePath;

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath));
        }

        public static void Play(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            string fullPath = ResolveAudioPath(filePath);
            if (!File.Exists(fullPath))
                return;

            try
            {
                var player = new MediaPlayer();

                lock (LockObj)
                {
                    ActivePlayers.Add(player);
                }

                bool isCleanedUp = false;
                void Cleanup()
                {
                    lock (LockObj)
                    {
                        if (isCleanedUp) return;
                        isCleanedUp = true;
                        ActivePlayers.Remove(player);
                    }
                    try
                    {
                        player.Close();
                    }
                    catch { }
                }

                player.MediaEnded += (s, e) => Cleanup();
                player.MediaFailed += (s, e) => Cleanup();

                player.Open(new Uri(fullPath, UriKind.Absolute));
                player.Play();
            }
            catch
            {
                // 静默处理音频启动异常
            }
        }
    }
}