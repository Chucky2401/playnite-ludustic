using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace LudusaviRestic
{
    public class LudusaviRestic : GenericPlugin
    {
        public LudusaviResticSettings settings { get; set; }
        private static readonly ILogger logger = LogManager.GetLogger();
        internal static IResourceProvider resources = new ResourceProvider();
        internal SsvViewSidebar ssvViewSidebar;

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        private ResticBackupManager manager;
        private Timer timer;

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        {
            this.settings = new LudusaviResticSettings(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            this.manager = new ResticBackupManager(this.settings, this.PlayniteApi);

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                ssvViewSidebar = new SsvViewSidebar(PlayniteApi, settings);
            }
        }

        public class SsvViewSidebar : SidebarItem
        {

            public SsvViewSidebar(IPlayniteAPI PlayniteApi, LudusaviResticSettings settings)
            {
                Process Backrest = new Process();
                Backrest.StartInfo.FileName = "D:\\Utilisateurs\\TheBlackWizard\\Logiciels\\Backrest\\backrest.exe";
                Backrest.StartInfo.CreateNoWindow = true;
                Backrest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                Type = SiderbarItemType.View;
                Title = resources.GetString("LOCLuduRestTitleViewBackrest");
                Icon = new TextBlock
                {
                    Text = "\uee00",
                    FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                };
                Opened = () =>
                {
                    Backrest.Start();

                    SidebarItemControl sidebarItemControl = new SidebarItemControl(PlayniteApi);
                    sidebarItemControl.SetTitle(resources.GetString("LOCLuduRestTitleViewBackrest"));
                    sidebarItemControl.AddContent(new BackrestView());

                    return sidebarItemControl;
                };
                Closed = () =>
                {
                    try
                    {
                        Backrest.Kill();
                    }
                    catch (Exception e)
                    {
                        logger.Debug(e, "Failed to Kill Backrest");
                    }

                    try
                    {
                        Backrest.Close();
                    }
                    catch (Exception e)
                    {
                        logger.Debug(e, "Failed to close Backrest");
                    }
                };
                Visible = settings.BackrestSidebar;
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            return new List<SidebarItem>
            {
                ssvViewSidebar
            };
        }

        private void LocalizeTags()
        {
            if (PlayniteApi.Database.Tags.Get(this.settings.ExcludeTagID) is Tag excludeTag)
            {
                excludeTag.Name = ResourceProvider.GetString("LOCLuduRestBackupExcludeTag");
            }
            if (PlayniteApi.Database.Tags.Get(this.settings.IncludeTagID) is Tag includeTag)
            {
                includeTag.Name = ResourceProvider.GetString("LOCLuduRestBackupIncludeTag");
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs menuArgs)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Backup all games",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM"),
                    Action = args => {
                        this.manager.BackupAllGames();
                    }
                }
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs menuArgs)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMCreate"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            this.manager.PerformManualBackup(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMIncludeAdd"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            AddTag(game, this.settings.IncludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMIncludeRemove"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            RemoveTag(game, this.settings.IncludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMExcludeAdd"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            AddTag(game, this.settings.ExcludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMExcludeRemove"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            RemoveTag(game, this.settings.ExcludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                }
            };
        }

        private bool AddTag(Game game, Guid tagId)
        {
            if (game is Game && tagId != Guid.Empty)
            {
                if (game.TagIds is List<Guid> ids)
                {
                    return ids.AddMissing(tagId);
                }
                else
                {
                    game.TagIds = new List<Guid> { tagId };
                }
            }
            return false;
        }

        private bool RemoveTag(Game game, Guid tagId)
        {
            if (game is Game && tagId != Guid.Empty)
            {
                if (game.TagIds is List<Guid> ids)
                {
                    return ids.Remove(tagId);
                }
            }
            return false;
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            LocalizeTags();
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            if (settings.BackupOnUninstall)
            {
                this.manager.PerformBackup(args.Game);
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args?.Game is null) return;

            if (settings.BackupDuringGameplay)
            {
                this.timer = new Timer(GameplayBackupTimerElapsed, args.Game,
                    settings.GameplayBackupInterval * 60000,
                    settings.GameplayBackupInterval * 60000);
            }
        }

        private void GameplayBackupTimerElapsed(Object obj)
        {
            Game game = (Game)obj;
            this.manager.PerformGameplayBackup(game);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            this.timer?.Dispose();

            if (this.settings.BackupWhenGameStopped)
            {
                string caption = ResourceProvider.GetString("LOCLuduRestGameStoppedPromptCaption");
                string message = ResourceProvider.GetString("LOCLuduRestGameStoppedPromptMessage");


                if (this.settings.PromptForGameStoppedTag)
                {
                    StringSelectionDialogResult result = PlayniteApi.Dialogs.SelectString(
                        $"{message}: {args.Game.Name}",
                        caption,
                        ""
                    );

                    if (result.Result)
                    {
                        logger.Debug($"Custom backup tag provided: {result.SelectedString}");
                    }

                    this.manager.PerformGameStoppedBackup(args.Game, result.SelectedString);
                }
                else
                {
                    this.manager.PerformGameStoppedBackup(args.Game);
                }
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LudusaviResticSettingsView(this);
        }
    }
}
