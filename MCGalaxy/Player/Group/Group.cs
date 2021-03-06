/*
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
using System.IO;
using MCGalaxy.Blocks;
using MCGalaxy.Commands;
using MCGalaxy.Events.GroupEvents;
using BlockID = System.UInt16;

namespace MCGalaxy {
    /// <summary> This is the group object, where ranks and their data are stored </summary>
    public sealed partial class Group {

        public static Group BannedRank { get { return Find(LevelPermission.Banned); } }
        public static Group GuestRank { get { return Find(LevelPermission.Guest); } }
        public static Group NobodyRank { get { return Find(LevelPermission.Nobody); } }
        public static Group standard;
        public static List<Group> GroupList = new List<Group>();
        static bool reloading;
        
        const int mapGenLimitAdmin = 225 * 1000 * 1000;
        const int mapGenLimit = 30 * 1000 * 1000;
        public static bool cancelrank = false;
        
        public string Name;
        public string Color;
        public string ColoredName { get { return Color + Name; } }
        public LevelPermission Permission = LevelPermission.Null;
        public int DrawLimit;
        public int MaxUndo;
        public string MOTD = "";
        public int GenVolume = mapGenLimit;
        public byte OverseerMaps = 3;
        public bool AfkKicked = true;
        public int AfkKickMinutes = 45;
        public string Prefix = "";   
        public int CopySlots = 1;
        internal string filename;
        
        public PlayerList Players;
        public List<Command> Commands;
        public bool[] Blocks = new bool[Block.ExtendedCount];
        public Group() { }
        
        private Group(LevelPermission perm, int maxB, int maxUn, string name, char colCode) {
            Permission = perm;
            DrawLimit = maxB;
            MaxUndo = maxUn;
            Name = name;
            Color = "&" + colCode;
            GenVolume = perm < LevelPermission.Admin ? mapGenLimit : mapGenLimitAdmin;
            AfkKicked = perm <= LevelPermission.AdvBuilder;
        }
        
        
        public void SetUsableCommands() {
            Commands = CommandPerms.AllCommandsUsableBy(Permission);
        }
        
        public void SetUsableBlocks() {
            for (int i = 0; i < Blocks.Length; i++) {
                Blocks[i] = BlockPerms.UsableBy(Permission, (BlockID)i);
            }
        }

        public bool CanExecute(Command cmd) { return Commands.Contains(cmd); }
        public bool CanExecute(string cmdName) {
            Command cmd = Command.all.Find(cmdName);
            return cmd != null && Commands.Contains(cmd);
        }

        /// <summary> Creates a copy of this group, except for members list and usable commands and blocks. </summary>
        public Group CopyConfig() {
            Group copy = new Group();
            copy.Name = Name; copy.Color = Color; copy.Permission = Permission;
            copy.DrawLimit = DrawLimit; copy.MaxUndo = MaxUndo; copy.MOTD = MOTD;
            copy.GenVolume = GenVolume; copy.OverseerMaps = OverseerMaps;
            copy.AfkKicked = AfkKicked; copy.AfkKickMinutes = AfkKickMinutes;
            copy.Prefix = Prefix; copy.CopySlots = CopySlots; copy.filename = filename;
            return copy;            
        }        
        
        
        public static Group Find(string name) {
            MapName(ref name);
            foreach (Group grp in GroupList) {
                if (grp.Name.CaselessEq(name)) return grp;
            }
            return null;
        }
        
        internal static void MapName(ref string name) {
            if (name.CaselessEq("op")) name = "operator";
        }

        public static Group Find(LevelPermission perm) {
            return GroupList.Find(grp => grp.Permission == perm);
        }

        public static Group GroupIn(string playerName) {
            foreach (Group grp in GroupList) {
                if (grp.Players.Contains(playerName)) return grp;
            }
            return standard;
        }
        
        public static string GetColoredName(LevelPermission perm) {
            Group grp = Find(perm);
            if (grp != null) return grp.ColoredName;
            return Colors.white + ((int)perm);
        }
        
        public static string GetColoredName(string rankName) {
            Group grp = Find(rankName);
            if (grp != null) return grp.ColoredName;
            return Colors.white + rankName;
        }
        
        public static string GetColor(LevelPermission perm) {
            Group grp = Find(perm);
            if (grp != null) return grp.Color;
            return Colors.white;
        }
        
        public static LevelPermission ParsePermOrName(string value, LevelPermission defPerm) {
            if (value == null) return defPerm;
            
            sbyte perm;
            if (sbyte.TryParse(value, out perm))
                return (LevelPermission)perm;
            
            Group grp = Find(value);
            return grp != null ? grp.Permission : defPerm;
        }
        
        
        public static void Register(Group grp) {
            GroupList.Add(grp);
            grp.LoadPlayers();
            
            if (reloading) {
                grp.SetUsableBlocks();
                grp.SetUsableCommands();
            }
            OnGroupLoadedEvent.Call(grp);
        }
        
        public static void InitAll() {
            GroupList = new List<Group>();
            if (File.Exists(Paths.RankPropsFile)) {
                GroupProperties.InitAll();
            } else {
                // Add some default ranks
                Register(new Group(LevelPermission.Builder, 400, 300, "Builder", '2'));
                Register(new Group(LevelPermission.AdvBuilder, 1200, 900, "AdvBuilder", '3'));
                Register(new Group(LevelPermission.Operator, 2500, 5400, "Operator", 'c'));
                Register(new Group(LevelPermission.Admin, 65536, int.MaxValue, "SuperOP", 'e'));
            }

            if (BannedRank == null)
                Register(new Group(LevelPermission.Banned, 1, 1, "Banned", '8'));
            if (GuestRank == null)
                Register(new Group(LevelPermission.Guest, 1, 120, "Guest", '7'));
            if (NobodyRank == null)
                Register(new Group(LevelPermission.Nobody, 65536, -1, "Nobody", '0'));
            
            GroupList.Sort((a, b) => a.Permission.CompareTo(b.Permission));
            standard = Find(ServerConfig.DefaultRankName);
            if (standard == null) standard = GuestRank;

            OnGroupLoadEvent.Call();
            reloading = true;
            SaveList(GroupList);
        }

        static readonly object saveLock = new object();
        public static void SaveList(List<Group> givenList) {
            lock (saveLock) {
                GroupProperties.SaveGroups(givenList);       
            }
            OnGroupSaveEvent.Call();
        }
        
        
        void LoadPlayers() {
            string desired = (int)Permission + "_rank";
            // Try to use the auto filename format
            if (filename == null || !filename.StartsWith(desired))
                MoveToDesired(desired);
            
            Players = PlayerList.Load("ranks/" + filename);
        }
        
        void MoveToDesired(string desired) {
            // rank doesn't exist to begin with
            if (filename == null || !File.Exists("ranks/" + filename)) {
                filename = desired + ".txt";
                // TODO: should start backwards from z to a
            } else if (MoveToFile(desired + ".txt")) {
            } else {
                // try appending a and z if duplicate file
                for (char c = 'a'; c <= 'z'; c++) {
                    string newFile = desired + c + ".txt";
                    if (MoveToFile(newFile)) return;
                }
            }
        }
        
        bool MoveToFile(string newFile) {
            if (File.Exists("ranks/" + newFile)) return false;
            
            try {
                File.Move("ranks/" + filename, "ranks/" + newFile);
                filename = newFile;
                return true;
            } catch (Exception ex) {
                Logger.LogError(ex);
                return false;
            }
        }
    }
}
