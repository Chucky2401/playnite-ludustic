using Playnite.SDK;
using System;

namespace LudusaviRestic
{
    public class BackupContext
    {
        public string NotificationID = "Lususavi Restic";
        private IPlayniteAPI api;
        private LudusaviResticSettings settings;

        public IPlayniteAPI API { get { return this.api; } }
        public LudusaviResticSettings Settings { get { return this.settings; } }

        public BackupContext(IPlayniteAPI api, LudusaviResticSettings settings)
        {
            this.api = api;
            this.settings = settings;
            ApplyEnvironment();
        }

        public void NotificationIDEventStopped()
        {
            NotificationID = "Lususavi Restic - Stopped";
        }

        public void NotificationIDEventGameplay()
        {
            NotificationID = "Lususavi Restic - Gameplay";
        }

        public void NotificationIDEventManual()
        {
            NotificationID = "Lususavi Restic - Manual";
        }

        public void ApplyEnvironment()
        {
            Environment.SetEnvironmentVariable("RCLONE_CONFIG_PASS", this.settings.RcloneConfigPassword);
            Environment.SetEnvironmentVariable("RESTIC_REPOSITORY", this.settings.ResticRepository);
            Environment.SetEnvironmentVariable("RESTIC_PASSWORD", this.settings.ResticPassword);
            Environment.SetEnvironmentVariable("RCLONE_CONFIG", this.settings.RcloneConfigPath);
        }
    }
}
