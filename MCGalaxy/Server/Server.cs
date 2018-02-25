/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using MCGalaxy.Commands;
using MCGalaxy.Commands.World;
using MCGalaxy.Drawing;
using MCGalaxy.Eco;
using MCGalaxy.Games;
using MCGalaxy.Network;
using MCGalaxy.Scripting;
using MCGalaxy.Tasks;
using MCGalaxy.Util;

namespace MCGalaxy {
    public sealed partial class Server {
        
        public Server() { Server.s = this; }
        
        //True = cancel event
        //Fale = dont cacnel event
        public static bool Check(string cmd, string message) {
            if (ConsoleCommand != null) ConsoleCommand(cmd, message);
            return cancelcommand;
        }
        
        [Obsolete("Use Logger.LogError(Exception)")]
        public static void ErrorLog(Exception ex) { Logger.LogError(ex); }
        
        [Obsolete("Use Logger.Log(LogType, String)")]
        public void Log(string message) { Logger.Log(LogType.SystemActivity, message); }
                
        [Obsolete("Use Logger.Log(LogType, String)")]
        public void Log(string message, bool systemMsg = false) {
            LogType type = systemMsg ? LogType.BackgroundActivity : LogType.SystemActivity;
            Logger.Log(type, message);
        }
        
        static void CheckFile(string file) {
            if (File.Exists(file)) return;
            
            Logger.Log(LogType.SystemActivity, file + " doesn't exist, Downloading..");
            try {
                using (WebClient client = HttpUtil.CreateWebClient()) {
                    client.DownloadFile(Updater.BaseURL + file + "?raw=true", file);
                }
                if (File.Exists(file)) {
                    Logger.Log(LogType.SystemActivity, file + " download succesful!");
                }
            } catch {
                Logger.Log(LogType.Warning, "Downloading {0} failed, please try again later", file);
            }
        }
        
        internal static ConfigElement[] serverConfig, levelConfig, zombieConfig, ctfConfig, zoneConfig;
        public static void Start() {
            serverConfig = ConfigElement.GetAll(typeof(ServerConfig));
            zombieConfig = ConfigElement.GetAll(typeof(ZSConfig));
            ctfConfig = ConfigElement.GetAll(typeof(ClassicCTFConfig));
            levelConfig = ConfigElement.GetAll(typeof(LevelConfig));
            zoneConfig = ConfigElement.GetAll(typeof(ZoneConfig));
            
            #pragma warning disable 0618
            Player.players = PlayerInfo.Online.list;
            Server.levels = LevelInfo.Loaded.list;
            #pragma warning restore 0618
            
            StartTime = DateTime.UtcNow;
            shuttingDown = false;
            Logger.Log(LogType.SystemActivity, "Starting Server");
            ServicePointManager.Expect100Continue = false;
            
            CheckFile("MySql.Data.dll");
            CheckFile("System.Data.SQLite.dll");
            CheckFile("sqlite3_x32.dll");
            CheckFile("sqlite3_x64.dll");
            CheckFile("Newtonsoft.Json.dll");
            CheckFile("LibNoise.dll");

            EnsureFilesExist();
            MoveSqliteDll();
            MoveOutdatedFiles();

            lava = new LavaSurvival();
            zombie = new ZSGame();
            ctfGame = new ClassicCTFGame();
            Countdown = new CountdownGame();
            LoadAllSettings();
            SrvProperties.GenerateSalt();

            InitDatabase();
            Economy.LoadDatabase();
            Server.zombie.CheckTableExists();
            Server.ctfGame.CheckTableExists();

            Background.QueueOnce(UpgradeTasks.CombineEnvFiles);
            Background.QueueOnce(LoadMainLevel);
            Plugin.LoadAll();
            Background.QueueOnce(UpgradeTasks.UpgradeOldBlacklist);
            Background.QueueOnce(LoadAutoloadMaps);
            Background.QueueOnce(UpgradeTasks.MovePreviousLevelFiles);
            Background.QueueOnce(UpgradeTasks.UpgradeOldTempranks);
            Background.QueueOnce(UpgradeTasks.UpgradeDBTimeSpent);
            Background.QueueOnce(LoadPlayerLists);
            Background.QueueOnce(UpgradeTasks.UpgradeOldLockdown);
            Background.QueueOnce(UpgradeTasks.UpgradeBots);
            
            Background.QueueOnce(SetupSocket);
            Background.QueueOnce(InitTimers);
            Background.QueueOnce(InitRest);
            Background.QueueOnce(InitHeartbeat);
            
            Devs.Clear();
            Mods.Clear();
            Background.QueueOnce(InitTasks.UpdateStaffList);

            ServerTasks.QueueTasks();
            Background.QueueRepeat(ThreadSafeCache.DBCache.CleanupTask,
                                   null, TimeSpan.FromMinutes(5));
        }
        
        static void MoveSqliteDll() {
            try {
                if (File.Exists("sqlite3_x32.dll") && IntPtr.Size == 4)
                    File.Copy("sqlite3_x32.dll", "sqlite3.dll", true);
                
                if (File.Exists("sqlite3_x64.dll") && IntPtr.Size == 8)
                    File.Copy("sqlite3_x64.dll", "sqlite3.dll", true);
            } catch { }
        }
        
        static void EnsureFilesExist() {
            EnsureDirectoryExists("properties");
            EnsureDirectoryExists("levels");
            EnsureDirectoryExists("bots");
            EnsureDirectoryExists("text");
            EnsureDirectoryExists("ranks");
            RankInfo.EnsureExists();
            Ban.EnsureExists();

            EnsureDirectoryExists("extra");
            EnsureDirectoryExists(Paths.WaypointsDir);
            EnsureDirectoryExists("extra/bots");
            EnsureDirectoryExists(Paths.ImportsDir);
            EnsureDirectoryExists("blockdefs");
            EnsureDirectoryExists(IScripting.DllDir);
            EnsureDirectoryExists(IScripting.SourceDir);
        }
        
        static void EnsureDirectoryExists(string dir) {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        
        static void MoveOutdatedFiles() {
            try {
                if (File.Exists("blocks.json")) File.Move("blocks.json", "blockdefs/global.json");
                if (File.Exists("server.properties")) File.Move("server.properties", Paths.ServerPropsFile);
                if (File.Exists("rules.txt")) File.Move("rules.txt", Paths.RulesFile);
                if (File.Exists("welcome.txt")) File.Move("welcome.txt", Paths.WelcomeFile);
                if (File.Exists("messages.txt")) File.Move("messages.txt", Paths.AnnouncementsFile);
                if (File.Exists("externalurl.txt")) File.Move("externalurl.txt", "text/externalurl.txt");
                if (File.Exists("autoload.txt")) File.Move("autoload.txt", "text/autoload.txt");
                if (File.Exists("IRC_Controllers.txt")) File.Move("IRC_Controllers.txt", "ranks/IRC_Controllers.txt");
                if (ServerConfig.WhitelistedOnly && File.Exists("whitelist.txt")) File.Move("whitelist.txt", "ranks/whitelist.txt");
            }
            catch { }
        }
        
        public static void LoadAllSettings() {
            // Unload custom plugins
            List<Plugin> plugins = Plugin.all;
            foreach (Plugin plugin in plugins) {
                if (Plugin.core.Contains(plugin)) continue;
                plugin.Unload(false);
            }
            
            zombie.LoadInfectMessages();
            Colors.LoadList();
            Alias.Load();
            BlockDefinition.LoadGlobal();
            ImagePalette.Load();
            
            SrvProperties.Load();
            Group.InitAll();
            Command.InitAll();
            CommandPerms.Load();
            Block.SetBlocks();
            Awards.Load();
            Economy.Load();
            WarpList.Global.Filename = "extra/warps.save";
            WarpList.Global.Load();
            CommandExtraPerms.Load();
            ProfanityFilter.Init();
            Team.LoadList();
            ChatTokens.LoadCustom();
            SrvProperties.FixupOldPerms();
            
            // Reload custom plugins
            foreach (Plugin plugin in plugins) {
                if (Plugin.core.Contains(plugin)) continue;
                plugin.Load(false);
            }
        }
        
        public static Thread Stop(bool restart) { return Stop(restart, ""); }
        public static Thread Stop(bool restart, string msg) {
            Server.shuttingDown = true;
            if (msg.Length == 0) {
                msg = restart ? "Server restarted. Sign in again and rejoin." : ServerConfig.DefaultShutdownMessage;
            }
            
            Exit(restart, msg);
            Thread stopThread = new Thread(() => ShutdownThread(restart, msg));
            stopThread.Start();
            return stopThread;
        }
        
        static void ShutdownThread(bool restarting, string msg) {
            try {
                Player[] players = PlayerInfo.Online.Items; 
                foreach (Player p in players) { p.Leave(msg); }
            } catch (Exception ex) { 
                Logger.LogError(ex); 
            }

            try {
                string autoload = null;
                Level[] loaded = LevelInfo.Loaded.Items;
                foreach (Level lvl in loaded) {
                    if (!lvl.ShouldSaveChanges()) continue;
                    
                    autoload = autoload + lvl.name + "=" + lvl.physics + Environment.NewLine;
                    lvl.Save(false, true);
                    lvl.SaveBlockDBChanges();
                }
                
                if (Server.ServerSetupFinished && !ServerConfig.AutoLoadMaps) {
                    File.WriteAllText("text/autoload.txt", autoload);
                }
            } catch (Exception ex) {
                Logger.LogError(ex); 
            }
            
            try { FileLogger.Flush(null); } catch { }
            if (restarting) Process.Start(RestartPath);
            Environment.Exit(0);
        }
        
        static void Exit(bool restarting, string msg) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player p in players) { p.save(); }
            foreach (Player p in players) { p.Leave(msg); }

            Player.connections.ForEach(p => p.Leave(msg));
            Plugin.UnloadAll();
            if (Listener != null) Listener.Close();
            
            try {
                IRC.Disconnect(restarting ? "Server is restarting." : "Server is shutting down.");
            } catch {
            }
        }

        public static void PlayerListUpdate() {
            if (OnPlayerListChange != null) OnPlayerListChange();
        }

        public static void UpdateUrl(string url) {
            if (OnURLChange != null) OnURLChange(url);
        }

        static void RandomMessage(SchedulerTask task) {
            if (PlayerInfo.Online.Count > 0 && messages.Count > 0) {
                Chat.MessageGlobal(messages[new Random().Next(0, messages.Count)]);
            }
        }

        internal static void SettingsUpdate() {
            if (OnSettingsUpdate != null) OnSettingsUpdate();
        }
        
        /// <summary> Sets the main level of the server that new players spawn in. </summary>
        /// <returns> true if main level was changed, false if not
        /// (same map as current main, or given map doesn't exist).</returns>
        public static bool SetMainLevel(string mapName) {
            if (mapName.CaselessEq(ServerConfig.MainLevel)) return false;
            Level oldMain = mainLevel;
            
            Level lvl = LevelInfo.FindExact(mapName);
            if (lvl == null)
                lvl = CmdLoad.LoadLevel(null, mapName);
            if (lvl == null) return false;
            
            oldMain.Config.AutoUnload = true;
            mainLevel = lvl;
            mainLevel.Config.AutoUnload = false;
            ServerConfig.MainLevel = mapName;
            return true;
        }
        
        public static void DoGC() {
            long start = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            long end = GC.GetTotalMemory(false);
            double deltaKB = (start - end) / 1024.0;
            if (deltaKB >= 100.0) {
                string track = (end / 1024.0).ToString("F2");
                string delta = deltaKB.ToString("F2");
                Logger.Log(LogType.BackgroundActivity, "GC performed (tracking {0} KB, freed {1} KB)", track, delta);
            }
        }
    }
}