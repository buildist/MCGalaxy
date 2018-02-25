using MCGalaxy.Games;
using System;
using System.Collections.Generic;

namespace MCGalaxy {
    public partial class Player {
        public const int maxFlamethrowerUnits = 100;
        public readonly LinkedList<Mine> mines = new LinkedList<Mine>();
        public bool isNewPlayer = false;
        public CTFTeam team = CTFTeam.Spectator;
        public int killstreak = 0;
        public DateTime safeTime;
        public bool hasTNT = false;
        public ushort tntX;
        public ushort tntY;
        public ushort tntZ;
        public ushort tntRadius = 2;
        public bool hasFlag = false;
        public int outOfBoundsBlockChanges = 0;
        public int placeBlock = -1;
        public bool placeSolid = false;
        public bool hasVoted = false;
        public bool hasNominated = false;
        public int bigTNTRemaining = 0;
        public Position linePosition;
        public Position lineRotation;
        public bool flamethrowerEnabled;
        public int flamethrowerUnits;
        public DateTime flamethrowerTime;
        public DateTime rocketTime;
        public int headBlockType;
        public Position headBlockPosition;
        public int accumulatedStorePoints = 0;
        public Player duelChallengedBy;
        public Player duelPlayer;
        public int duelKills = 0;
        public int bountySet = 0;
        public Player bountied;
        public Player bountiedBy;
        public int bountyKills = 0;
        public int bountyAmount = 0;
        public bool bountyMode = false;
        public int lastAmount = 0;
        public bool bountyActive = false;
        public ChatMode chatMode = ChatMode.Default;
        public Player chatPlayer;
        public bool sendCommandLog = false;
        public CTFStats ctfStats;

        public bool IsOp {
            get {
                return Rank > LevelPermission.Operator;
            }
        }

        public bool IsVIP {
            get {
                return Rank > LevelPermission.AdvBuilder;
            }
        }

        public void RemoveMine(Mine m) {
            lock (mines) {
                mines.Remove(m);
            }
        }

        public void ClearMines() {
            lock (mines) {
                foreach (Mine m in mines) {
                    level.RemoveMine(m);
                    level.SetTile(m.x, m.y, m.z, 0);
                }
                mines.Clear();
            }
        }

        public bool CanKill(Player p, bool sendMessage) {
            if (duelPlayer == null && p != duelPlayer) {
                if (sendMessage)
                    SendMessage("- &eYou can't kill " + p.ColoredName + " &esince you are dueling " + duelPlayer.ColoredName
                        + "&e. Only they can hurt you right now.");
                return false;
            } else if(duelPlayer == null && p.duelPlayer != null) {
                if (sendMessage)
                    SendMessage("- &eYou can't kill " + p.ColoredName + " &esince they are dueling " + p.duelPlayer.ColoredName
                        + "&e. They can't capture your flag or kill anyone else right now.");
                return false;
            } else if(p.team == CTFTeam.Spectator) {
                if (sendMessage)
                    SendMessage("- &eYou can't kill " + p.ColoredName + " &esince they are spectating.");
                return false;
            }
            return true;
        }

        public void OnKill(Player defender) {
            if (defender.team == CTFTeam.Spectator || defender.team == team) return;

            killstreak++;
            ctfStats.maxKillstreak = Math.Max(ctfStats.maxKillstreak, killstreak);
            if (killstreak % 5 == 0)
                Chat.MessageGlobal("- {0} &bhas a killstreak of {1}!", ColoredName, killstreak);
            if (duelPlayer == defender) {
                duelKills++;
                if (duelKills == 3) {
                    Chat.MessageGlobal("- {0} &bhas defeated {1} &b in a duel!", ColoredName, duelPlayer.ColoredName);
                    ctfStats.duelWins++;
                    duelPlayer.ctfStats.duelLosses++;
                    duelChallengedBy = null;
                    duelPlayer.duelChallengedBy = null;
                    duelPlayer.duelPlayer = null;
                    duelPlayer = null;
                    SendToTeamSpawn();
                }
            }
        }

        public void OnKilledBy(Player attacker) {
            if (killstreak >= 10) {
                Chat.MessageGlobal("- {0} &bended {1}&b's killstreak of {2}", attacker.ColoredName, ColoredName, killstreak);
            }
            killstreak = 0;
            attacker.ctfStats.maxKillstreakEnded = Math.Max(ctfStats.maxKillstreak, killstreak);
            ctfStats.deaths++;
            Server.ctfGame.CheckForUnbalance(this);
            if (bountyMode) {
                if (team == CTFTeam.Spectator) {
                    bountiedBy.ctfStats.storePoints += bountyAmount;
                    bountied = null;
                    bountiedBy = null;
                    bountyMode = false;
                } else if (this != attacker && bountiedBy != attacker && bountied == this) {
                    attacker.bountyKills++;
                    if (attacker.bountyKills == attacker.lastAmount + 5) {
                        Chat.MessageGlobal(
                            "- {0} &bhas collected the bounty of {1} on {2}&b!",
                            attacker.ColoredName, bountyAmount, ColoredName);
                        attacker.ctfStats.storePoints += bountyAmount;
                        bountied = null;
                        bountiedBy = null;
                        bountyMode = false;
                        bountyAmount = 0;
                        attacker.lastAmount = attacker.bountyKills;
                    }
                }

            }
        }

        public void MarkSafe() {
            safeTime = DateTime.UtcNow;
        }

        public bool IsSafe() {
            return (DateTime.UtcNow - safeTime).TotalSeconds <= 3;
        }

        public void AutoJoinTeam() {
            string team;
            if (Server.ctfGame.redPlayers > Server.ctfGame.bluePlayers)
                team = "blue";
            else if (Server.ctfGame.redPlayers < Server.ctfGame.bluePlayers)
                team = "red";
            else
                team = new Random().NextDouble() < 0.5 ? "red" : "blue";
            JoinTeam(team);
        }

        public void JoinTeam(String team) {
            JoinTeam(team, true);
        }

        public void JoinTeam(String team, bool sendMessage) {
            ClassicCTFGame ctf = Server.ctfGame;
            if (this.team == CTFTeam.Spectator && team != "spec") {
                SendMessage("- &aThis map was contributed by: " + Level.Config.CTFMapAuthor);
            }
            if (ctf.voting) return;
            if (this.team == CTFTeam.Red) ctf.redPlayers--;
            else if (this.team == CTFTeam.Blue) ctf.bluePlayers--;
            int diff = ctf.redPlayers - ctf.bluePlayers;
            bool unbalanced = (diff >= 1 && team == "red") || (diff <= -1 && team == "blue");
            bool invalid = false;
            if (hasFlag) {
                if (this.team == CTFTeam.Red) {
                    ctf.blueFlagTaken = false;
                    ctf.PlaceBlueFlag();
                } else {
                    ctf.redFlagTaken = false;
                    ctf.PlaceRedFlag();
                }
                hasFlag = false;
                Chat.MessageGlobal("- {0} &edropped the flag!");
            }
            if (team == "red") {
                if (unbalanced && ctf.redPlayers > ctf.bluePlayers) {
                    ctf.bluePlayers++;
                    this.team = CTFTeam.Blue;
                    team = "blue";
                    SendMessage("- &eRed team is full");
                } else {
                    ctf.redPlayers++;
                    this.team = CTFTeam.Red;
                }
            } else if (team == "blue") {
                if (unbalanced && ctf.bluePlayers > ctf.redPlayers) {
                    ctf.redPlayers++;
                    this.team = CTFTeam.Red;
                    team = "red";
                    SendMessage("- &Blue team is full");
                } else {
                    ctf.bluePlayers++;
                    this.team = CTFTeam.Blue;
                }
            } else if (team == "spec") {
                this.team = CTFTeam.Spectator;
                if (duelPlayer != null) {
                    duelPlayer.duelPlayer = null;
                    duelPlayer = null;
                }
            } else {
                invalid = true;

                SendMessage("- &eUnrecognized team!");
            }
            ClearMines();

            if (!invalid) {
                if (sendMessage) Chat.MessageGlobal("- {0} &ejoined the {1} team", ColoredName, team);
                SendToTeamSpawn();
            }
            if (isNewPlayer) {
                ctfStats.rules = true;
                isNewPlayer = false;
            }
        }

        public void SendToTeamSpawn() {
            Position spawn = Level.GetTeamSpawn(this.team);
            SendPos(Entities.SelfID, spawn, new Orientation((byte)(this.team == CTFTeam.Red ? 64 : 192), 0));
        }

        public static Player GetPlayer(string name, Player source) {
            Player player = null;
            foreach (Player p in source.Level.players) {
                if (p.name.ToLower().Contains(name.ToLower())) {
                    if (player == null) {
                        player = p;
                    } else {
                        player = null;
                        break;
                    }
                }
            }
            return player;
        }
    }
}
