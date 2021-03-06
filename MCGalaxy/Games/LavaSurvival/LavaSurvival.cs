/*
    Copyright 2011 MCForge
        
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
using System.Text;
using System.Timers;
using MCGalaxy.Commands.World;
using MCGalaxy.Maths;

namespace MCGalaxy.Games
{
    public sealed partial class LavaSurvival
    {
        // Private variables
        private string propsPath = "properties/lavasurvival/";
        private List<string> maps, voted;
        private Dictionary<string, int> votes, deaths;
        private Random rand = new Random();
        private Timer announceTimer, voteTimer, transferTimer;
        private DateTime startTime;

        // Public variables
        public bool active = false, roundActive = false, flooded = false, voteActive = false, sendingPlayers = false;
        public Level map;
        public MapSettings mapSettings;
        public MapData mapData;
        
        /// <summary> Gets whether lava survival is currently running. </summary>
        public override bool Running { get { return active; } }

        // Settings
        public bool startOnStartup, sendAfkMain = true;
        public byte voteCount = 2;
        public int lifeNum = 3;
        public double voteTime = 2;

        // Constructors
        public LavaSurvival() {
            maps = new List<string>();
            voted = new List<string>();
            votes = new Dictionary<string, int>();
            deaths = new Dictionary<string, int>();
            announceTimer = new Timer(60000);
            announceTimer.AutoReset = true;
            announceTimer.Elapsed += delegate
            {
                if (!flooded) AnnounceTimeLeft(true, false);
            };
            LoadSettings();
        }

        // Public methods
        public byte Start(string mapName = "")
        {
            if (active) return 1; // Already started
            if (maps.Count < 3) return 2; // Not enough maps
            if (!String.IsNullOrEmpty(mapName) && !HasMap(mapName)) return 3; // Map doesn't exist

            deaths.Clear();
            active = true;
            Logger.Log(LogType.GameActivity, "[Lava Survival] Game started.");
            
            try { LoadMap(String.IsNullOrEmpty(mapName) ? maps[rand.Next(maps.Count)] : mapName); }
            catch (Exception e) { Logger.LogError(e); active = false; return 4; }
            return 0;
        }
        public byte Stop()
        {
            if (!active) return 1; // Not started

            active = false;
            roundActive = false;
            voteActive = false;
            flooded = false;
            deaths.Clear();
            if (announceTimer.Enabled) announceTimer.Stop();
            try { mapData.Dispose(); }
            catch { }
            try { voteTimer.Dispose(); }
            catch { }
            try { transferTimer.Dispose(); }
            catch { }
            map.Unload(true, false);
            map = null;
            Logger.Log(LogType.GameActivity, "[Lava Survival] Game stopped.");
            return 0;
        }

        public void StartRound()
        {
            if (roundActive) return;

            try
            {
                deaths.Clear();
                mapData.roundTimer.Elapsed += delegate { EndRound(); };
                mapData.floodTimer.Elapsed += delegate { DoFlood(); };
                mapData.roundTimer.Start();
                mapData.floodTimer.Start();
                announceTimer.Start();
                startTime = DateTime.UtcNow;
                roundActive = true;
                Logger.Log(LogType.GameActivity, "[Lava Survival] Round started. Map: " + map.ColoredName);
            }
            catch (Exception e) { Logger.LogError(e); }
        }

        public override void EndRound()
        {
            if (!roundActive) return;

            roundActive = false;
            flooded = false;
            try
            {
                try { mapData.Dispose(); }
                catch { }
                map.SetPhysics(5);
                map.ChatLevel("The round has ended!");
                Logger.Log(LogType.GameActivity, "[Lava Survival] Round ended. Voting...");
                StartVote();
            }
            catch (Exception e) { Logger.LogError(e); }
        }

        public void DoFlood()
        {
            if (!active || !roundActive || flooded || map == null) return;
            flooded = true;

            try
            {
                announceTimer.Stop();
                map.ChatLevel("&4Look out, here comes the flood!");
                Logger.Log(LogType.GameActivity, "[Lava Survival] Map flooding.");
                if (mapData.layer)
                {
                    DoFloodLayer();
                    mapData.layerTimer.Elapsed += delegate
                    {
                        if (mapData.currentLayer <= mapSettings.layerCount)
                        {
                            DoFloodLayer();
                        }
                        else
                            mapData.layerTimer.Stop();
                    };
                    mapData.layerTimer.Start();
                }
                else
                {
                    map.Blockchange(mapSettings.blockFlood.X, mapSettings.blockFlood.Y, mapSettings.blockFlood.Z, mapData.block, true);
                }
            }
            catch (Exception e) { Logger.LogError(e); }
        }

        void DoFloodLayer()  {
            Logger.Log(LogType.GameActivity, "[Lava Survival] Layer " + mapData.currentLayer + " flooding.");
            map.Blockchange(mapSettings.blockLayer.X, (ushort)(mapSettings.blockLayer.Y + ((mapSettings.layerHeight * mapData.currentLayer) - 1)), mapSettings.blockLayer.Z, mapData.block, true);
            mapData.currentLayer++;
        }

        public void AnnounceTimeLeft(bool flood, bool round, Player p = null, bool console = false) {
            if (!active || !roundActive || startTime == null || map == null) return;

            if (flood) {
                double floodMinutes = Math.Ceiling((startTime.AddMinutes(mapSettings.floodTime) - DateTime.UtcNow).TotalMinutes);
                if (p == null && !console) map.ChatLevel("&3" + floodMinutes + " minute" + (floodMinutes == 1 ? "" : "s") + " %Suntil the flood.");
                else Player.Message(p, "&3" + floodMinutes + " minute" + (floodMinutes == 1 ? "" : "s") + " %Suntil the flood.");
            }
            if (round) {
                double roundMinutes = Math.Ceiling((startTime.AddMinutes(mapSettings.roundTime) - DateTime.UtcNow).TotalMinutes);
                if (p == null && !console) map.ChatLevel("&3" + roundMinutes + " minute" + (roundMinutes == 1 ? "" : "s") + " %Suntil the round ends.");
                else Player.Message(p, "&3" + roundMinutes + " minute" + (roundMinutes == 1 ? "" : "s") + " %Suntil the round ends.");
            }
        }

        public void AnnounceRoundInfo(Player p = null, bool console = false)  {
            if (p == null && !console) {
                if (mapData.water) map.ChatLevel("The map will be flooded with &9water %Sthis round!");
                if (mapData.layer)
                {
                    map.ChatLevel("The " + (mapData.water ? "water" : "lava") + " will &aflood in layers %Sthis round!");
                    map.ChatLevelOps("There will be " + mapSettings.layerCount + " layers, each " + mapSettings.layerHeight + " blocks high.");
                    map.ChatLevelOps("There will be another layer every " + mapSettings.layerInterval + " minutes.");
                }
                if (mapData.fast) map.ChatLevel("The lava will be &cfast %Sthis round!");
                if (mapData.killer) map.ChatLevel("The " + (mapData.water ? "water" : "lava") + " will &ckill you %Sthis round!");
                if (mapData.destroy) map.ChatLevel("The " + (mapData.water ? "water" : "lava") + " will &cdestroy plants " + (mapData.water ? "" : "and flammable blocks ") + "%Sthis round!");
            } else {
                if (mapData.water) Player.Message(p, "The map will be flooded with &9water %Sthis round!");
                if (mapData.layer) Player.Message(p, "The " + (mapData.water ? "water" : "lava") + " will &aflood in layers %Sthis round!");
                if (mapData.fast) Player.Message(p, "The lava will be &cfast %Sthis round!");
                if (mapData.killer) Player.Message(p, "The " + (mapData.water ? "water" : "lava") + " will &ckill you %Sthis round!");
                if (mapData.destroy) Player.Message(p, "The " + (mapData.water ? "water" : "lava") + " will &cdestroy plants " + (mapData.water ? "" : "and flammable blocks ") + "%Sthis round!");
            }
        }

        public void LoadMap(string name)
        {
            if (String.IsNullOrEmpty(name) || !HasMap(name)) return;

            name = name.ToLower();
            Level oldMap = null;
            if (active && map != null) oldMap = map;
            CmdLoad.LoadLevel(null, name);
            map = LevelInfo.FindExact(name);

            if (map != null)
            {
                mapSettings = LoadMapSettings(name);
                mapData = GenerateMapData(mapSettings);

                map.SetPhysics(mapData.destroy ? 2 : 1);
                map.Config.PhysicsOverload = 1000000;
                map.Config.AutoUnload = false;
                map.Config.LoadOnGoto = false;
                Level.SaveSettings(map);
            }
            
            if (active && map != null)
            {
                sendingPlayers = true;
                try
                {
                    Player[] online = PlayerInfo.Online.Items; 
                    foreach (Player pl in online) {
                        pl.Game.RatedMap = false;
                        pl.Game.PledgeSurvive = false;
                        if (pl.level == oldMap)
                        {
                            if (sendAfkMain && pl.IsAfk) 
                                PlayerActions.ChangeMap(pl, Server.mainLevel);
                            else 
                                PlayerActions.ChangeMap(pl, map);
                        }
                    }
                    oldMap.Unload(true, false);
                }
                catch { }
                sendingPlayers = false;

                StartRound();
            }
        }

        public void StartVote()
        {
            if (maps.Count < 3) return;

            // Make sure these are cleared or bad stuff happens!
            votes.Clear();
            voted.Clear();

            byte i = 0;
            string opt, str = "";
            while (i < Math.Min(voteCount, maps.Count - 1))
            {
                opt = maps[rand.Next(maps.Count)];
                if (!votes.ContainsKey(opt) && opt != map.name)
                {
                    votes.Add(opt, 0);
                    str += "%S, &5" + opt.Capitalize();
                    i++;
                }
            }

            map.ChatLevel("Vote for the next map! The vote ends in " + voteTime + " minute" + (voteTime == 1 ? "" : "s") +".");
            map.ChatLevel("Choices: " + str.Remove(0, 4));

            voteTimer = new Timer(TimeSpan.FromMinutes(voteTime).TotalMilliseconds);
            voteTimer.AutoReset = false;
            voteTimer.Elapsed += delegate
            {
                try {
                    EndVote();
                    voteTimer.Dispose();
                }
                catch (Exception e) { Logger.LogError(e); }
            };
            voteTimer.Start();
            voteActive = true;
        }
        
        List<string> GetVotedLevels() {
            var keys = votes.Keys;
            List<string> names = new List<string>();
            foreach (string key in keys) 
                names.Add(key);
            return names;
        }

        public void EndVote() {
            if (!voteActive) return;

            voteActive = false;
            Logger.Log(LogType.GameActivity, "[Lava Survival] Vote ended.");
            KeyValuePair<string, int> most = new KeyValuePair<string, int>(String.Empty, -1);
            foreach (KeyValuePair<string, int> kvp in votes)
            {
                if (kvp.Value > most.Value) most = kvp;
                map.ChatLevelOps("&5" + kvp.Key.Capitalize() + "&f: &a" + kvp.Value);
            }
            votes.Clear();
            voted.Clear();

            map.ChatLevel("The vote has ended! &5" + most.Key.Capitalize() + " %Swon with &a" + most.Value + " %Svote" + (most.Value == 1 ? "" : "s") + ".");
            map.ChatLevel("You will be transferred in 5 seconds...");
            transferTimer = new Timer(5000);
            transferTimer.AutoReset = false;
            transferTimer.Elapsed += delegate
            {
                try
                {
                    LoadMap(most.Key);
                    transferTimer.Dispose();
                }
                catch (Exception e) { Logger.LogError(e); }
            };
            transferTimer.Start();
        }

        public bool AddVote(Player p, string vote)
        {
            if (!voteActive || voted.Contains(p.name) || !votes.ContainsKey(vote)) return false;
            int temp = votes[vote] + 1;
            votes.Remove(vote);
            votes.Add(vote, temp);
            voted.Add(p.name);
            return true;
        }

        public bool HasVote(string vote)
        {
            return voteActive && votes.ContainsKey(vote);
        }

        public bool HasPlayer(Player p)
        {
            return p.level == map;
        }
        public void KillPlayer(Player p, bool silent = false)
        {
            if (lifeNum < 1) return;
            string name = p.name.ToLower();
            if (!deaths.ContainsKey(name))
                deaths.Add(name, 0);
            deaths[name]++;
            
            if (!silent && IsPlayerDead(p))
            {
                Player[] online = PlayerInfo.Online.Items; 
                foreach (Player pl in online) {
                    if (pl != p && HasPlayer(pl))
                        Player.Message(pl, p.ColoredName + " &4ran out of lives, and is out of the round!");
                }
                Player.Message(p, "&4You ran out of lives, and are out of the round!");
                Player.Message(p, "&4You can still watch, but you cannot build.");
            }
        }
        public bool IsPlayerDead(Player p)
        {
            string name = p.name.ToLower();
            if (lifeNum < 1 || !deaths.ContainsKey(name))
                return false;
            return (deaths[name] >= lifeNum);
        }

        public void AddMap(string name)
        {
            if (!String.IsNullOrEmpty(name) && !HasMap(name))
            {
                maps.Add(name.ToLower());
                SaveSettings();
            }
        }
        public void RemoveMap(string name)
        {
            if (maps.CaselessRemove(name))
            {
                SaveSettings();
            }
        }
        public bool HasMap(string name)
        {
            return maps.CaselessContains(name);
        }

        public bool InSafeZone(Vec3U16 pos)
        {
            return InSafeZone(pos.X, pos.Y, pos.Z);
        }

        public bool InSafeZone(ushort x, ushort y, ushort z)
        {
            if (mapSettings == null) return false;
            return x >= mapSettings.safeZone[0].X && x <= mapSettings.safeZone[1].X && y >= mapSettings.safeZone[0].Y 
                && y <= mapSettings.safeZone[1].Y && z >= mapSettings.safeZone[0].Z && z <= mapSettings.safeZone[1].Z;
        }

        public List<string> Maps
        {
            get
            {
                return new List<string>(maps);
            }
        }
    }
}
