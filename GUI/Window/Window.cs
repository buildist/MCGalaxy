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
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Generator;
using MCGalaxy.Tasks;

namespace MCGalaxy.Gui {
    public partial class Window : Form {
        // for cross thread use
        delegate void StringCallback(string s);
        delegate void PlayerListCallback(List<Player> players);
        delegate void VoidDelegate();
        bool mapgen = false;

        PlayerCollection pc;
        LevelCollection lc;
        public NotifyIcon notifyIcon = new NotifyIcon();
        Player curPlayer;

        public Window() {
            InitializeComponent();
        }

        void Window_Load(object sender, EventArgs e) {
            main_btnProps.Enabled = false;
            MaximizeBox = false;
            Text = "Starting " + Server.SoftwareNameVersioned + "...";
            Show();
            BringToFront();
            WindowState = FormWindowState.Normal;

            InitServer();
            foreach (string theme in MapGen.SimpleThemeNames) {
                map_cmbType.Items.Add(theme);
            }
            
            Text = ServerConfig.Name + " - " + Server.SoftwareNameVersioned;
            MakeNotifyIcon();
            
            // Bind player list
            main_Players.DataSource = pc;
            main_Players.Font = new Font("Calibri", 8.25f);

            main_Maps.DataSource = new LevelCollection(); // Otherwise "-1 does not have a value" exception when clicking a row
            main_Maps.Font = new Font("Calibri", 8.25f);
        }
        
        void UpdateNotifyIconText() {
            int playerCount = PlayerInfo.Online.Count;
            string players = " (" + playerCount + " players)";
            
            // ArgumentException thrown if text length is > 63
            string text = (ServerConfig.Name + players);
            if (text.Length > 63) text = text.Substring(0, 63);
            notifyIcon.Text = text;
        }
        
        void MakeNotifyIcon() {
            UpdateNotifyIconText();
            notifyIcon.ContextMenuStrip = icon_context;
            notifyIcon.Icon = Icon;
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += notifyIcon_MouseClick;
        }

        void notifyIcon_MouseClick(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) openConsole_Click(sender, e);
        }
        
        void InitServer() {
            Server s = new Server();
            Logger.LogHandler += LogMessage;
            Updater.NewerVersionDetected += LogNewerVersionDetected;

            Server.OnURLChange += UpdateUrl;
            Server.OnPlayerListChange += UpdateClientList;
            Server.OnSettingsUpdate += SettingsUpdate;
            Server.Background.QueueOnce(InitServerTask);
        }
        
        void LogMessage(LogType type, string message) {
            if (InvokeRequired) {
                BeginInvoke((Action<LogType, string>)LogMessage, type, message);
                return;
            }           
            if (Server.shuttingDown) return;
            string newline = Environment.NewLine;
            
            switch (type) {
                case LogType.Error:
                    main_txtLog.AppendLog("!!!Error! See " + FileLogger.ErrorLogPath + " for more information." + newline);
                    message = FormatError(message);
                    logs_txtError.AppendText(message + newline);
                    break;
                case LogType.BackgroundActivity:
                    message = DateTime.Now.ToString("(HH:mm:ss) ") + message;
                    logs_txtSystem.AppendText(message + newline);
                    break;
                case LogType.CommandUsage:
                    message = DateTime.Now.ToString("(HH:mm:ss) ") + message;
                    main_txtLog.AppendLog(message + newline, main_txtLog.ForeColor, false);
                    break;
                default:
                    main_txtLog.AppendLog(message + newline);
                    break;
            }
        }
        
        static string FormatError(string message) {
            string date = "----" + DateTime.Now + "----";
            return date + Environment.NewLine + message + Environment.NewLine + "-------------------------";
        }

        static volatile bool msgOpen = false;
        static void LogNewerVersionDetected(object sender, EventArgs e) {
            if (msgOpen) return;
            // don't want message box blocking background scheduler thread
            Thread thread = new Thread(ShowUpdateMessageAsync);
            thread.Name = "MCGalaxy_UpdateMsgBox";
            thread.Start();
        }
        
        static void ShowUpdateMessageAsync() {
            msgOpen = true;
            if (MessageBox.Show("New version found. Would you like to update?", "Update?", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                Updater.PerformUpdate();
            }
            msgOpen = false;
        }
        
        void InitServerTask(SchedulerTask task) {
            Server.Start();
            // The first check for updates is run after 10 seconds, subsequent ones every two hours
            Server.Background.QueueRepeat(Updater.UpdaterTask, null, TimeSpan.FromSeconds(10));

            OnPlayerConnectEvent.Register(Player_PlayerConnect, Priority.Low);
            OnPlayerDisconnectEvent.Register(Player_PlayerDisconnect, Priority.Low);
            OnJoinedLevelEvent.Register(Player_OnJoinedLevel, Priority.Low);

            OnLevelAddedEvent.Register(Level_LevelAdded, Priority.Low);
            OnLevelRemovedEvent.Register(Level_LevelRemoved, Priority.Low);
            OnPhysicsLevelChangedEvent.Register(Level_PhysicsLevelChanged, Priority.Low);

            RunOnUI_Async(() => main_btnProps.Enabled = true);
        }

        public void RunOnUI_Async(Action act) { BeginInvoke(act); }
        
        void Player_PlayerConnect(Player p) {
            UpdatePlayers();
        }
        
        void Player_PlayerDisconnect(Player p, string reason) {
            UpdatePlayers();
        }
        
        void Player_OnJoinedLevel(Player p, Level prevLevel, Level lvl) {
            RunOnUI_Async(() => {
                UpdateMapList();
                UpdatePlayerSelected(); 
            });
        }
        
        void Level_LevelAdded(Level lvl) {
            RunOnUI_Async(() => {
                UpdateMapList();
                UpdateUnloadedList();
            });
        }
        
        void Level_LevelRemoved(Level lvl) {
            RunOnUI_Async(() => {
                UpdateMapList();
                UpdateUnloadedList();
            });
        }
        
        void Level_PhysicsLevelChanged(Level lvl, int level) {
            RunOnUI_Async(() => {
                UpdateMapList();
            });
        }


        void SettingsUpdate() {
            if (Server.shuttingDown) return;
            
            if (main_txtLog.InvokeRequired) {
                Invoke(new VoidDelegate(SettingsUpdate));
            } else {
                Text = ServerConfig.Name + " - " + Server.SoftwareNameVersioned;
                UpdateNotifyIconText();
            }
        }
        
        delegate void LogDelegate(string message);

        /// <summary> Updates the list of client names in the window </summary>
        /// <param name="players">The list of players to add</param>
        public void UpdateClientList() {
            if (InvokeRequired) { Invoke(new VoidDelegate(UpdateClientList)); return; }            
            UpdateNotifyIconText();
            Player[] players = PlayerInfo.Online.Items;

            // Try to keep the same selection on update
            string selected = null;
            var selectedRows = main_Players.SelectedRows;
            if (selectedRows.Count > 0) {
                selected = (string)selectedRows[0].Cells[0].Value;
            }

            // Update the data source and control
            pc = new PlayerCollection();          
            foreach (Player pl in players) { pc.Add(pl); }
            main_Players.DataSource = pc;
            
            // Reselect player
            if (selected != null) {
                foreach (DataGridViewRow row in main_Players.Rows) {
                    string name = (string)row.Cells[0].Value;
                    if (name.CaselessEq(selected)) row.Selected = true;
                }
            }
            main_Players.Refresh();
        }

        public void PopupNotify(string message, ToolTipIcon icon = ToolTipIcon.Info) {
            notifyIcon.ShowBalloonTip(3000, ServerConfig.Name, message, icon);
        }

        void UpdateMapList() {
            Level[] loaded = LevelInfo.Loaded.Items;
            
            // Try to keep the same selection on update
            string selected = null;
            var selectedRows = main_Maps.SelectedRows;
            if (selectedRows.Count > 0) {
                selected = (string)selectedRows[0].Cells[0].Value;
            }
            
            // Update the data source and control
            lc = new LevelCollection();
            foreach (Level lvl in loaded) { lc.Add(lvl); }
            main_Maps.DataSource = lc;            
            
            // Reselect map
            if (selected != null) {
                foreach (DataGridViewRow row in main_Maps.Rows) {
                    string name = (string)row.Cells[0].Value;
                    if (name.CaselessEq(selected)) row.Selected = true;
                }
            }
            main_Maps.Refresh();
                        
            
            // Try to keep the same selection on update
            selected = null;
            if (map_lbLoaded.SelectedItem != null) {
                selected = map_lbLoaded.SelectedItem.ToString();
            }
            
            map_lbLoaded.Items.Clear();
            foreach (Level lvl in loaded) {
                map_lbLoaded.Items.Add(lvl.name);
            }
            
            if (selected != null) {
                int index = map_lbLoaded.Items.IndexOf(selected);
                map_lbLoaded.SelectedIndex = index;
            } else {
                map_lbLoaded.SelectedIndex = -1;
            }
            UpdateSelectedMap(null, null);
        }

        /// <summary> Places the server's URL at the top of the window </summary>
        /// <param name="s">The URL to display</param>
        public void UpdateUrl(string s) {
            if (InvokeRequired) {
                StringCallback d = UpdateUrl;
                Invoke(d, new object[] { s });
            } else {
                main_txtUrl.Text = s;
            }
        }

        void Window_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.WindowsShutDown) {
                Server.Stop(false, "Server shutdown - PC turning off");
                notifyIcon.Dispose();
            }
            
            if (Server.shuttingDown || MessageBox.Show("Really shutdown the server? All players will be disconnected!", "Exit", MessageBoxButtons.OKCancel) == DialogResult.OK) {
                if (!Server.shuttingDown) Server.Stop(false);
                notifyIcon.Dispose();
            } else {
                // Prevents form from closing when user clicks the X and then hits 'cancel'
                e.Cancel = true;
            }
        }

        void btnClose_Click(object sender, EventArgs e) { Close(); }

        void btnProperties_Click(object sender, EventArgs e) {
            if (!prevLoaded) { PropertyForm = new PropertyWindow(); prevLoaded = true; }
            PropertyForm.Show();
            if (!PropertyForm.Focused) PropertyForm.Focus();
        }

        public static bool prevLoaded = false;
        Form PropertyForm;

        void Window_Resize(object sender, EventArgs e) {
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
        }

        void openConsole_Click(object sender, EventArgs e) {
            Show();
            BringToFront();
            WindowState = FormWindowState.Normal;
        }

        void shutdownServer_Click(object sender, EventArgs e) {
            Close();
        }      

       void tabs_Click(object sender, EventArgs e)  {
            try { UpdateUnloadedList(); }
            catch { }
            try { UpdatePlayers(); }
            catch { }
            
            try {
                if (logs_txtGeneral.Text.Length == 0)
                    logs_dateGeneral.Value = DateTime.Now;
            } catch { }
            
            foreach (TabPage page in tabs.TabPages)
                foreach (Control control in page.Controls)
            {
                if (!control.GetType().IsSubclassOf(typeof(TextBox))) continue;
                control.Update();
            }
            tabs.Update();
        }

        void icon_restart_Click(object sender, EventArgs e) {
            main_BtnRestart_Click(sender, e);
        }

        void main_players_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e) {
            e.PaintParts &= ~DataGridViewPaintParts.Focus;
        }
    }
}
