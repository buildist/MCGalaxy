﻿/*
Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
Dual-licensed under the Educational Community License, Version 2.0 and
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MCGalaxy.Blocks;
using MCGalaxy.DB;
using MCGalaxy.Drawing;
using MCGalaxy.Events.EconomyEvents;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Games;
using MCGalaxy.SQL;
using MCGalaxy.Network;
using MCGalaxy.Tasks;
using MCGalaxy.Maths;
using BlockID = System.UInt16;
using BlockRaw = System.Byte;

namespace MCGalaxy {
    public class ChatMessage {
        public string text { get; set; }
        public string time { get; set; }
        public string username { get; set; }
    }
    
    public partial class Player : Entity, IDisposable {

        static int sessionCounter;

        //This is so that plugin devs can declare a player without needing a socket..
        //They would still have to do p.Dispose()..
        public Player(string playername) { 
            name = playername;
            truename = playername;
            DisplayName = playername;
            SessionID = Interlocked.Increment(ref sessionCounter) & SessionIDMask;
            SuperUser = true;
        }

        internal Player() {
            spamChecker = new SpamChecker(this);
            SessionID = Interlocked.Increment(ref sessionCounter) & SessionIDMask;
            for (int i = 0; i < BlockBindings.Length; i++) {
                BlockBindings[i] = Block.FromRaw((byte)i);
            }
        }
        
        internal void Connect(Socket s) {
            try {
                Socket = new TcpSocket(this, s);
                ip = Socket.RemoteIP;
                Socket.RegisterCallbacks();
                
                Logger.Log(LogType.UserActivity, ip + " connected to the server.");
                Socket.ReceiveNextAsync();
                connections.Add(this);
            } catch ( Exception e ) {
                Leave("Login failed!"); Logger.LogError(e);
            }
        }
        
        public override byte EntityID { get { return id; } }
        public override Level Level { get { return level; } }
        
        public override bool CanSeeEntity(Entity other) {
            Player target = other as Player;
            if (target == null) return true; // not a player
            
            bool mayBeHidden = target.hidden;
            mayBeHidden |= (target.Game.Referee || target.Game.Invisible) && Server.zombie.Running;
            if (!mayBeHidden || this == other) return true;
            
            if (target.Game.Referee && !Game.Referee && Server.zombie.Running) return false;
            if (target.Game.Invisible && !Game.Referee && Server.zombie.Running) return false;
            
            if (target.otherRankHidden) return Rank >= target.oHideRank;
            return Rank >= target.Rank;
        }        
        
        public BlockID GetHeldBlock() {
            if (ModeBlock != Block.Air) return ModeBlock;
            return BlockBindings[(BlockRaw)RawHeldBlock];
        }
        
        public void SetPrefix() {
            prefix = Game.Referee ? "&2[Ref] " : "";
            if (group.Prefix.Length > 0) prefix += "&f" + group.Prefix + color;
            Team team = Game.Team;
            prefix += team != null ? "<" + team.Color + team.Name + color + "> " : "";
            
            IGame game = level == null ? null : level.CurrentGame();
            if (game != null) game.AdjustPrefix(this, ref prefix);
            
            bool isDev = Server.Devs.CaselessContains(truename);
            bool isMod = Server.Mods.CaselessContains(truename);
            bool devPrefix = ServerConfig.SoftwareStaffPrefixes;
            
            string viptitle =
                (devPrefix && isMod) ? string.Format("{0}[&aInfo{0}] ", color) :
                (devPrefix && isDev) ? string.Format("{0}[&9Dev{0}] ", color)  : "";
            prefix = prefix + viptitle;
            prefix = (title.Length == 0) ? prefix : prefix + color + "[" + titlecolor + title + color + "] ";
        }
        
        public bool CheckIfInsideBlock() {
            AABB bb = ModelBB.OffsetPosition(Pos);
            return AABB.IntersectsSolidBlocks(bb, level);
        }

        public void save() {
            OnSQLSaveEvent.Call(this);
            if (cancelmysql) { cancelmysql = false; return; }

            // Player disconnected before SQL data was retrieved
            if (!gotSQLData) return;
            long blocks = PlayerData.BlocksPacked(TotalPlaced, TotalModified);
            long cuboided = PlayerData.CuboidPacked(TotalDeleted, TotalDrawn);
            Database.Backend.UpdateRows("Players", "IP=@0, LastLogin=@1, totalLogin=@2, totalDeaths=@3, Money=@4, " +
                                        "totalBlocks=@5, totalCuboided=@6, totalKicked=@7, TimeSpent=@8, Messages=@9", "WHERE Name=@10", 
                                        ip, LastLogin.ToString("yyyy-MM-dd HH:mm:ss"),
                                        TimesVisited, TimesDied, money, blocks,
                                        cuboided, TimesBeenKicked, (long)TotalTime.TotalSeconds, TotalMessagesSent, name);
            
            Server.zombie.SaveZombieStats(this);
            Server.ctfGame.SaveCTFStats(this);
        }

        #region == GLOBAL MESSAGES ==
        
        public static void GlobalBlockchange(Level level, int b, BlockID block) {
            ushort x, y, z;
            level.IntToPos(b, out x, out y, out z);
            GlobalBlockchange(level, x, y, z, block);
        }
        
        public static void GlobalBlockchange(Level level, ushort x, ushort y, ushort z, BlockID block) {
            Player[] players = PlayerInfo.Online.Items; 
            foreach (Player p in players) { 
                if (p.level == level) p.SendBlockchange(x, y, z, block);
            }
        }
        
        public static void SendChatFrom(Player from, string message) { SendChatFrom(from, message, true); }
        public static void SendChatFrom(Player from, string message, bool showname) {
            if (from == null) return;            
            if (Last50Chat.Count == 50) Last50Chat.RemoveAt(0);
            ChatMessage chatmessage = new ChatMessage();
            chatmessage.text = message;
            chatmessage.username = from.color + from.name;
            chatmessage.time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            Last50Chat.Add(chatmessage);
            
            Chat.MessageGlobal(from, message, showname, false);
        }

        public static List<ChatMessage> Last50Chat = new List<ChatMessage>();
        public static void GlobalIRCMessage(string srcNick, string message) {
            message = Colors.Escape(message);
            Player[] players = PlayerInfo.Online.Items; 
            foreach (Player p in players) {
                if (p.Ignores.All || p.Ignores.IRC || p.Ignores.IRCNicks.Contains(srcNick)) continue;
                
                if (p.level.SeesServerWideChat && p.Chatroom == null)
                    Player.Message(p, message);
            }
        }

        public bool MarkPossessed(string marker = "") {
            if (marker.Length > 0) {
                Player controller = PlayerInfo.FindExact(marker);
                if (controller == null) return false;
                marker = " (" + controller.color + controller.name + color + ")";
            }
            
            Entities.GlobalDespawn(this, true);
            Entities.GlobalSpawn(this, true, marker);
            return true;
        }

        #endregion
        #region == DISCONNECTING ==
        
        /// <summary> Disconnects the players from the server, 
        /// with their default logout message shown in chat. </summary>
        public void Disconnect() { LeaveServer(PlayerDB.GetLogoutMessage(this), "disconnected", false); }
        
        /// <summary> Kicks the player from the server,
        /// with the given messages shown in chat and in the disconnect packet. </summary>
        public void Kick(string chatMsg, string discMsg, bool sync = false) {
            LeaveServer(chatMsg, discMsg, true, sync); 
        }

        /// <summary> Kicks the player from the server,
        /// with the given message shown in both chat and in the disconnect packet. </summary>
        public void Kick(string discMsg) { Kick(discMsg, false); }
        public void Kick(string discMsg, bool sync = false) {
            string chatMsg = discMsg;
            if (chatMsg.Length > 0) chatMsg = "(" + chatMsg + ")"; // old format
            LeaveServer(chatMsg, discMsg, true, sync);
        }
        
        /// <summary> Disconnects the players from the server,
        /// with the given message shown in both chat and in the disconnect packet. </summary>
        public void Leave(string chatMsg, string discMsg, bool sync = false) { 
            LeaveServer(chatMsg, discMsg, false, sync); 
        }

        /// <summary> Disconnects the players from the server,
        /// with the given messages shown in chat and in the disconnect packet. </summary>        
        public void Leave(string discMsg) { Leave(discMsg, false); }       
        public void Leave(string discMsg, bool sync = false) {
            LeaveServer(discMsg, discMsg, false, sync);
        }
        
        [Obsolete("Use Leave() or Kick() instead")]
        public void leftGame(string discMsg = "") {
            string chatMsg = discMsg;
            if (chatMsg.Length > 0) chatMsg = "(" + chatMsg + ")"; // old format
            LeaveServer(chatMsg, discMsg, true); 
        }
        

        bool leftServer = false;
        void LeaveServer(string chatMsg, string discMsg, bool isKick, bool sync = false) {
            if (leftServer) return;
            leftServer = true;
            CriticalTasks.Clear();
            ZoneIn = null;
            
            //Umm...fixed?
            if (name == null || name.Length == 0) {
                if (Socket != null) CloseSocket();
                connections.Remove(this);
                disconnected = true;
                Logger.Log(LogType.UserActivity, "{0} disconnected.", ip);
                return;
            }
            
            Server.reviewlist.Remove(name);
            try { 
                if (disconnected) {
                    CloseSocket();
                    connections.Remove(this);
                    PlayerInfo.Online.Remove(this);
                    return;
                }
                // FlyBuffer.Clear();
                LastAction = DateTime.UtcNow;
                IsAfk = false;
                isFlying = false;
                aiming = false;
                
                if (chatMsg != null) chatMsg = Colors.Escape(chatMsg);
                discMsg = Colors.Escape(discMsg);
                
                string kickPacketMsg = ChatTokens.Apply(discMsg, this);
                Send(Packet.Kick(kickPacketMsg, hasCP437), sync);
                disconnected = true;
                ZoneIn = null;
                if (isKick) TimesBeenKicked++;
                
                if (!loggedIn) {
                    connections.Remove(this);
                    RemoveFromPending();
                    PlayerInfo.Online.Remove(this);
                    
                    string user = name + " (" + ip + ")";
                    Logger.Log(LogType.UserActivity, "{0} disconnected. ({1})", user, discMsg);
                    return;
                }

                if ( Game.team != null ) Game.team.RemoveMember(this);
                TntWarsGame tntwarsgame = TntWarsGame.GameIn(this);
                if ( tntwarsgame != null ) {
                    tntwarsgame.Players.Remove(tntwarsgame.FindPlayer(this));
                    tntwarsgame.SendAllPlayersMessage("TNT Wars: " + ColoredName + " %Shas left TNT Wars!");
                }
                Server.ctfGame.PlayerDisconnected(this);

                Entities.DespawnEntities(this, false);
                ShowDisconnectInChat(chatMsg, isKick);
                save();

                PlayerInfo.Online.Remove(this);
                Server.PlayerListUpdate();
                OnPlayerDisconnectEvent.Call(this, discMsg);
                
                if (ServerConfig.AutoLoadMaps && level.Config.AutoUnload && !level.IsMuseum && !level.HasPlayers())
                    level.Unload(true);
                Dispose();
            } catch (Exception e) { 
                Logger.LogError(e); 
            } finally {
                CloseSocket();
            }
        }
        
        void ShowDisconnectInChat(string chatMsg, bool isKick) {
            if (chatMsg == null) return;
            
            if (!isKick) {
                string leavem = "&c- " + FullName + " %S" + chatMsg;
                const LevelPermission perm = LevelPermission.Guest;
                if (group.Permission > perm || (ServerConfig.GuestLeavesNotify && group.Permission <= perm)) {
                    Chat.MessageGlobal(this, leavem, false, true);
                }
                Logger.Log(LogType.UserActivity, "{0} disconnected ({1}%S).", name, chatMsg);
            } else {
                Chat.MessageGlobal(this, "&c- " + FullName + " %Skicked %S" + chatMsg, false);
                Logger.Log(LogType.UserActivity, "{0} kicked ({1}%S).", name, chatMsg);
            }
        }

        public void Dispose() {
            connections.Remove(this);
            RemoveFromPending();
            Extras.Clear();
            
            foreach (CopyState cState in CopySlots) { 
                if (cState != null) cState.Clear();
            }
            CopySlots.Clear();
            DrawOps.Clear();
            if (spamChecker != null)
                spamChecker.Clear();
            spyChatRooms.Clear();
        }

        #endregion
        #region == OTHER ==
        
        [Obsolete("Use PlayerInfo.Online.Items")]
        public static List<Player> players;

        public static bool ValidName(string name) {
            const string valid = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890._+";
            foreach (char c in name) {
                if (valid.IndexOf(c) == -1) return false;
            }
            return true;
        }
        
        public void SetCurrentCopy(CopyState state) {
            while (CurrentCopySlot >= CopySlots.Count) { CopySlots.Add(null); }
            CopySlots[CurrentCopySlot] = state;
        }

        #endregion

        public void BlockUntilLoad(int sleep) {
            while (Loading) 
                Thread.Sleep(sleep);
        }
        
        /// <summary> Sends a block change packet to the user containing the current block at the given coordinates. </summary>
        /// <remarks> Vanilla client always assumes block place/delete succeeds, so this method is usually used to echo back the
        /// old block. (e.g. due to insufficient permission to change that block, used as mark for draw operations) </remarks>
        public void RevertBlock(ushort x, ushort y, ushort z) {
            SendBlockchange(x, y, z, level.GetBlock(x, y, z));
        }
   
        public void SetMoney(int amount) {
            money = amount;
            OnMoneyChangedEvent.Call(this);
        }

        public void TntAtATime() {
            CurrentAmountOfTnt++;
            int delay = 0;

            switch (TntWarsGame.GameIn(this).Difficulty) {
                case TntWarsGame.TntWarsDifficulty.Easy:
                    delay = 3250; break;
                case TntWarsGame.TntWarsDifficulty.Normal:
                    delay = 2250; break;
                case TntWarsGame.TntWarsDifficulty.Hard:
                case TntWarsGame.TntWarsDifficulty.Extreme:
                    delay = 1250; break;
            }
            Server.MainScheduler.QueueOnce(AllowMoreTntTask, null, 
                                           TimeSpan.FromMilliseconds(delay));
        }
        
        void AllowMoreTntTask(SchedulerTask task) {
            CurrentAmountOfTnt--;
        }
        
        internal static bool CheckVote(string message, Player p, string a, string b, ref int totalVotes) {
            if (!p.voted && (message == a || message == b)) {
                totalVotes++;
                Player.Message(p, Colors.red + "Thanks for voting!");
                p.voted = true;
                return true;
            }
            return false;
        }
        
        public void CheckForMessageSpam() {
            if (spamChecker != null) spamChecker.CheckChatSpam();
        }        
        
        string selTitle;
        readonly object selLock = new object();
        Vec3S32[] selMarks;
        object selState;
        SelectionHandler selCallback;
        SelectionMarkHandler selMarkCallback;
        int selIndex;

        public void MakeSelection(int marks, string title, object state, 
                                  SelectionHandler callback, SelectionMarkHandler markCallback = null) {
            lock (selLock) {
                selMarks = new Vec3S32[marks];
                selTitle = title;
                selState = state;
                selCallback = callback;
                selMarkCallback = markCallback;
                selIndex = 0;
                Blockchange = SelectionBlockChange;
                if (title != null) InitSelectionHUD();
            }
        }
        
        public void MakeSelection(int marks, object state, SelectionHandler callback) {
            MakeSelection(marks, null, state, callback);
        }
        
        public void ClearSelection() {
            lock (selLock) {
                if (selTitle != null) ResetSelectionHUD();
                selTitle = null;
                selState = null;
                selCallback = null;
                selMarkCallback = null;
                Blockchange = null;
            }
        }
        
        void SelectionBlockChange(Player p, ushort x, ushort y, ushort z, BlockID block) {
            lock (selLock) {
                Blockchange = SelectionBlockChange;
                RevertBlock(x, y, z);
                
                selMarks[selIndex] = new Vec3S32(x, y, z);
                if (selMarkCallback != null) selMarkCallback(p, selMarks, selIndex, selState, block);
                // Mark callback cancelled selection
                if (selCallback == null) return;
                
                selIndex++;                
                if (selIndex == 1 && selTitle != null) {
                    SendCpeMessage(CpeMessageType.BottomRight2, "Mark #1" + FormatSelectionMark(selMarks[0]));
                } else if (selIndex == 2 && selTitle != null) {
                    SendCpeMessage(CpeMessageType.BottomRight1, "Mark #2" + FormatSelectionMark(selMarks[0]));
                }
                if (selIndex != selMarks.Length) return;
                
                string title = selTitle;
                object state = selState;
                SelectionMarkHandler markCallback = selMarkCallback;
                SelectionHandler callback = selCallback;
                ClearSelection();

                if (!Block.IsPhysicsType(block)) block = p.BlockBindings[(BlockRaw)block];
                bool canRepeat = callback(this, selMarks, state, block);
                
                if (canRepeat && staticCommands) {
                    MakeSelection(selIndex, title, state, callback, markCallback);
                }
            }
        }
        
        string FormatSelectionMark(Vec3S32 P) {
            return ": %S(" + P.X + ", " + P.Y + ", " + P.Z + ")";
        }
        
        void InitSelectionHUD() {
            SendCpeMessage(CpeMessageType.BottomRight3, selTitle);
            SendCpeMessage(CpeMessageType.BottomRight2, "Mark #1: %S(Not yet set)");
            string mark2Msg = selMarks.Length >= 2 ? "Mark #2: %S(Not yet set)" : "";
            SendCpeMessage(CpeMessageType.BottomRight1, mark2Msg);
        }
        
        void ResetSelectionHUD() {
            SendCpeMessage(CpeMessageType.BottomRight3, "");
            SendCpeMessage(CpeMessageType.BottomRight2, "");
            SendCpeMessage(CpeMessageType.BottomRight1, "");
        }        
    }
}
