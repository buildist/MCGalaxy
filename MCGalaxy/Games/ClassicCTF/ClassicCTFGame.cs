using MCGalaxy.SQL;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace MCGalaxy.Games {
    public partial class ClassicCTFGame : IGame {

        public ushort blockSpawnX;
        public ushort blockSpawnY;
        public ushort blockSpawnZ;

        public ushort redFlagX;
        public ushort redFlagY;
        public ushort redFlagZ;
        public ushort blueFlagX;
        public ushort blueFlagY;
        public ushort blueFlagZ;

        public bool redFlagDropped = false;
        public bool blueFlagDropped = false;
        private Thread redFlagDroppedThread;
        private Thread blueFlagDroppedThread;

        public int redCaptures;
        public int blueCaptures;

        public bool redFlagTaken = false;
        public bool blueFlagTaken = false;

        public bool antiStalemate;

        public int bluePlayers = 0;
        public int redPlayers = 0;

        public long gameStartTime;

        public Level startNewMap;
        public bool voting = false;
        public bool isFirstBlood = true;
        public bool ready = false;
        public List<string> rtvYesPlayers = new List<string>();
        public List<string> rtvNoPlayers = new List<string>();
        public int rtvVotes = 0;
        public List<string> nominatedMaps = new List<string>();
        public String currentMap = null;
        public String previousMap = null;

        private Level Level {
            get {
                return Server.mainLevel;
            }
        }

        public int RedPlayers {
            get {
                int count = 0;
                foreach(Player p in Level.players) {
                    if (p.team == CTFTeam.Red) count++;
                }
                return count;
            }
        }

        public int BluePlayers {
            get {
                int count = 0;
                foreach (Player p in Level.players) {
                    if (p.team == CTFTeam.Blue) count++;
                }
                return count;
            }
        }

        public override void PlayerJoinedLevel(Player player, Level level, Level oldLevel) {
            if (oldLevel == Level && level != Level) {
                PlayerDisconnected(player);
                return;
            }
            if (level != Level) return;

            String rank = player.ctfStats.rank != 0 ? " &f(Rank " + player.ctfStats.rank + ", " + player.ctfStats.games + " games played)" : "";
            Chat.MessageGlobal("&a{0} joined the game", player.name);
            if (!player.isNewPlayer) {
                player.SendMessage("&a" + ClassicCTFConfig.CTFWelcomeMessage);
                player.SendMessage("&bSay /join to start playing, or /spec to spectate");
                player.SendMessage("&bSay /help to learn how to play");
                player.SendMessage("&bSay /rules to read the rules");
            } else {
                player.SendMessage("&aWelcome to Capture the Flag! Here's how you play.");
                player.SendMessage("&e" + ClassicCTFConfig.HelpText);
                player.SendMessage("&bSay /join to start playing, or /spec to spectate. /help will show these instructions again.");
            }
        }

        public void PlayerDisconnected(Player p) {
            if (p.hasFlag) {
                DropFlag(p);
            }
            p.ClearMines();

            if (p.team == CTFTeam.Red)
                redPlayers--;
            else if (p.team == CTFTeam.Blue)
                bluePlayers--;
            if (p.duelPlayer != null) {
                p.duelPlayer.duelPlayer = null;
                p.duelPlayer = null;
            }
            Chat.MessageGlobal("&a{0} left the game", p.name);
            if (Level.players.Count == 0) {
                rtvVotes = 0;
            }
        }

        public void ShowScore() {
            Chat.MessageGlobal("- Current score: Red has {0} captures; blue has {1} captures", redCaptures, blueCaptures);
        }

        public void PlaceRedFlag() {
            Level.Blockchange(redFlagX, redFlagY, redFlagZ, Block.CTF_Flag_Red);
        }

        public void PlaceBlueFlag() {
            Level.Blockchange(redFlagX, redFlagY, redFlagZ, Block.CTF_Flag_Blue);
        }

        public void SetRedFlagPos(ushort x, ushort y, ushort z) {
            redFlagX = x;
            redFlagY = y;
            redFlagZ = z;
        }

        public void SetBlueFlagPos(ushort x, ushort y, ushort z) {
            blueFlagX = x;
            blueFlagY = y;
            blueFlagZ = z;
        }

        public void ResetRedFlagPos() {
            redFlagX = (ushort) Level.Config.CTFRedFlagX;
            redFlagY = (ushort) Level.Config.CTFRedFlagY;
            redFlagZ = (ushort) Level.Config.CTFRedFlagZ;
        }

        public void ResetBlueFlagPos() {
            blueFlagX = (ushort)Level.Config.CTFBlueFlagX;
            blueFlagY = (ushort)Level.Config.CTFBlueFlagY;
            blueFlagZ = (ushort)Level.Config.CTFBlueFlagZ;
        }

        public void OpenSpawns() {
            Level.Blockchange((ushort)Level.Config.CTFRedSpawnX, (ushort)(Level.Config.CTFRedSpawnY - 2), (ushort) Level.Config.CTFRedSpawnZ, Block.Air);
            Level.Blockchange((ushort)Level.Config.CTFBlueSpawnX, (ushort)(Level.Config.CTFBlueSpawnY - 2), (ushort)Level.Config.CTFBlueSpawnZ, Block.Air);
        }

        public void StartGame(Level newMap) {

        }

        public void EndGame() {

        }

        private void CheckFirstBlood(Player attacker, Player defender) {
            if (isFirstBlood && defender.team != CTFTeam.Spectator) {
                Chat.MessageGlobal("- {0} &4took the first blood!");
                attacker.ctfStats.tags += 10;
                attacker.ctfStats.storePoints += 50;
                isFirstBlood = false;
            }
        }

        public void CheckForStalemate() {
            if (redFlagTaken && blueFlagTaken) {
                Chat.MessageGlobal("- &eAnti-stalemate mode activated!");
                Chat.MessageGlobal("- &eIf your teammate gets tagged you'll drop the flag");
                antiStalemate = true;
            }
        }

        public void CheckForUnbalance(Player p) {
            if (redPlayers < bluePlayers - 2 && p.team == CTFTeam.Blue) {
                Chat.MessageGlobal("- {0} was moved to red team for game balance.", p.name);
                p.JoinTeam("red");
            } else if (bluePlayers < redPlayers - 2 && p.team == CTFTeam.Red) {
                Chat.MessageGlobal("- {0} was moved to blue team for game balance.", p.name);
                p.JoinTeam("blue");
            }

        }

        public void DropFlag(CTFTeam team) {
            foreach (Player p in Level.players) {
                if (p.team == team) {
                    DropFlag(p);
                }
            }
            antiStalemate = false;
        }

        public void DropFlag(Player p) {
            DropFlag(p, false, false);
        }

        public void DropFlag(Player p, bool instant, bool isVoluntary) {
            if (p.hasFlag) {
                p.hasFlag = false;
                Chat.MessageGlobal("- {0} &edropped the flag!");
                if (p.team == CTFTeam.Red) {
                    blueFlagTaken = false;
                    blueFlagDropped = true;
                    SetBlueFlagPos((ushort)p.Pos.BlockX, (ushort)p.Pos.BlockY, (ushort)p.Pos.BlockZ);
                    blueFlagDroppedThread = new Thread(delegate () {
                        try {
                            if ((!antiStalemate && !instant) || isVoluntary)
                                Thread.Sleep(10 * 1000);
                            Level.Blockchange(blueFlagX, blueFlagY, blueFlagZ, 0);
                            ResetBlueFlagPos();
                            PlaceBlueFlag();
                            Chat.MessageGlobal("- &eThe blue flag has been returned!");
                        } catch(ThreadInterruptedException ex) {
                            return;
                        }
                    });
                    blueFlagDroppedThread.Start();
                } else {
                    redFlagTaken = false;
                    redFlagDropped = true;
                    SetRedFlagPos((ushort)p.Pos.BlockX, (ushort)p.Pos.BlockY, (ushort)p.Pos.BlockZ);
                    redFlagDroppedThread = new Thread(delegate () {
                        try {
                            if ((!antiStalemate && !instant) || isVoluntary)
                                Thread.Sleep(10 * 1000);
                            Level.Blockchange(redFlagX, redFlagY, redFlagZ, 0);
                            ResetRedFlagPos();
                            PlaceRedFlag();
                            Chat.MessageGlobal("- &eThe red flag has been returned!");
                        } catch (ThreadInterruptedException ex) {
                            return;
                        }
                    });
                    redFlagDroppedThread.Start();
                }
            }
        }

        #region Database
        static ColumnDesc[] createSyntax = new ColumnDesc[] {
            new ColumnDesc("ID", ColumnType.Integer, priKey: true, autoInc: true, notNull: true),
            new ColumnDesc("Name", ColumnType.Char, 64),
            new ColumnDesc("CTFTags", ColumnType.Int32),
            new ColumnDesc("CTFExplodes", ColumnType.Int32),
            new ColumnDesc("CTFMines", ColumnType.Int32),
            new ColumnDesc("CTFCaptures", ColumnType.Int32),
            new ColumnDesc("CTFWins", ColumnType.Int32),
            new ColumnDesc("CTFGames", ColumnType.Int32),
            new ColumnDesc("CTFStorePoints", ColumnType.Int32),
            new ColumnDesc("CTFRagequits", ColumnType.Int32),
            new ColumnDesc("CTFDuelWins", ColumnType.Int32),
            new ColumnDesc("CTFDuelLosses", ColumnType.Int32),
            new ColumnDesc("CTFDeaths", ColumnType.Int32),
            new ColumnDesc("CTFMaxKillstreak", ColumnType.Int32),
            new ColumnDesc("CTFMaxKillstreakEnded", ColumnType.Int32),
            new ColumnDesc("CTFDomination", ColumnType.Int32),
            new ColumnDesc("CTFRevenge", ColumnType.Int32),
            new ColumnDesc("CTFStalemateTags", ColumnType.Int32),
            new ColumnDesc("CTFRank", ColumnType.Int32),
            new ColumnDesc("CTFRules", ColumnType.Bool)
        };

        public void CheckTableExists() {
            Database.Backend.CreateTable("CTFStats", createSyntax);
        }

        public CTFStats LoadCTFStats(string name) {
            DataTable table = Database.Backend.GetRows("CTFStats", "*", "WHERE Name=@0", name);
            CTFStats stats = default(CTFStats);
            if (table.Rows.Count > 0) {
                DataRow row = table.Rows[0];
                stats.tags = int.Parse(row["CTFTags"].ToString());
                stats.explodes = int.Parse(row["CTFExplodes"].ToString());
                stats.mines = int.Parse(row["CTFMines"].ToString());
                stats.captures = int.Parse(row["CTFCaptures"].ToString());
                stats.wins = int.Parse(row["CTFWins"].ToString());
                stats.games = int.Parse(row["CTFGames"].ToString());
                stats.storePoints = int.Parse(row["CTFStorePoints"].ToString());
                stats.ragequits = int.Parse(row["CTFRagequits"].ToString());
                stats.duelWins = int.Parse(row["CTFDuelWins"].ToString());
                stats.duelLosses = int.Parse(row["CTFDuelLosses"].ToString());
                stats.deaths = int.Parse(row["CTFDeaths"].ToString());
                stats.maxKillstreak = int.Parse(row["CTFMaxKillstreak"].ToString());
                stats.maxKillstreakEnded = int.Parse(row["CTFMaxKillstreakEnded"].ToString());
                stats.domination = int.Parse(row["CTFDomination"].ToString());
                stats.revenge = int.Parse(row["CTFRevenge"].ToString());
                stats.stalemateTags = int.Parse(row["CTFStalemateTags"].ToString());
                stats.rank = int.Parse(row["CTFRank"].ToString());
                stats.rules = bool.Parse(row["CTFRules"].ToString());
            }
            table.Dispose();
            return stats;
        }

        public void SaveCTFStats(Player p) {
            int count = 0;
            using (DataTable table = Database.Backend.GetRows("CTFStats", "*", "WHERE Name=@0", p.name)) {
                count = table.Rows.Count;
            }
            if (count == 0) {
                Database.Backend.AddRow
                    ("CTFStats", "CTFTags", "CTFExplodes, CTFMines, CTFCaptures, CTFWins," +
                    "CTFGames, CTFStorePoints, CTFRagequits, CTFDuelWins, CTFDuelLosses," +
                    "CTFDeaths, CTFMaxKillstreak, CTFMaxKillstreakEnded, CTFDomination," +
                    "CTFRevenge, CTFStalemateTags", "CTFRank", "CTFRules", "Name",
                    p.ctfStats.tags, p.ctfStats.explodes, p.ctfStats.mines, p.ctfStats.captures, p.ctfStats.wins,
                    p.ctfStats.games, p.ctfStats.storePoints, p.ctfStats.ragequits, p.ctfStats.duelWins,
                    p.ctfStats.duelLosses, p.ctfStats.deaths, p.ctfStats.maxKillstreak,
                    p.ctfStats.maxKillstreakEnded, p.ctfStats.domination, p.ctfStats.revenge,
                    p.ctfStats.stalemateTags, p.ctfStats.rules, p.name);
            } else {
                Database.Backend.UpdateRows
                    ("CTFStats", "CTFTags=@0, CTFExplodes=@1, CTFMines=@2, CTFCaptures=@3, CTFWins=@4," +
                    "CTFGames=@5, CTFStorePoints=@6, CTFRagequits=@7, CTFDuelWins=@8, CTFDuelLosses=@9," +
                    "CTFDeaths=@10, CTFMaxKillstreak=@11, CTFMaxKillstreakEnded=@12, CTFDomination=@13," +
                    "CTFRevenge=@14, CTFStalemateTags=@15", "CTFRank=@16", "CTFRules=@17", "WHERE NAME=@18",
                    p.ctfStats.tags, p.ctfStats.explodes, p.ctfStats.mines, p.ctfStats.captures, p.ctfStats.wins,
                    p.ctfStats.games, p.ctfStats.storePoints, p.ctfStats.ragequits, p.ctfStats.duelWins,
                    p.ctfStats.duelLosses, p.ctfStats.deaths, p.ctfStats.maxKillstreak,
                    p.ctfStats.maxKillstreakEnded, p.ctfStats.domination, p.ctfStats.revenge,
                    p.ctfStats.stalemateTags, p.ctfStats.rank, p.ctfStats.rules, p.name);

            }
        }
        #endregion

        public override bool Running {
            get {
                return true;
            }
        }

        public override void EndRound() {
            throw new NotImplementedException();
        }

    }

    public enum CTFTeam {
        Spectator, Red, Blue
    }

    public enum ChatMode {
        Default, Team, Operator, Private
    }

    public struct CTFStats {
        public int
            tags, explodes, mines, captures, wins, games,
            storePoints, ragequits, duelWins, duelLosses,
            deaths, maxKillstreak, maxKillstreakEnded,
            domination, revenge, stalemateTags, rank;
        public bool rules;
    }
}