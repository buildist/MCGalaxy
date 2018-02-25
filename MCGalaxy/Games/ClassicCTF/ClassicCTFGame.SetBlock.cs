using System;
using System.Threading;
using BlockID = System.UInt16;

namespace MCGalaxy.Games
{
    public partial class ClassicCTFGame
    {
        public bool SetBlock(Player player, Level level, ushort x, ushort y, ushort z, bool mode, BlockID type) {
            ushort oldType = level.GetBlock(x, y, z);
            int playerX = player.Pos.BlockX;
            int playerY = player.Pos.BlockY;
            int playerZ = player.Pos.BlockZ;
            int MAX_DISTANCE = 10;
            bool ignore = false;

            if (!mode) type = Block.Air;

            if (player.placeBlock != -1 && (player.placeBlock != Block.Bedrock || (player.placeBlock == Block.Bedrock && type == Block.Stone))) {
                type = (ushort) player.placeBlock;
            }
            if (player.placeSolid && type == Block.Stone) {
                type = Block.Bedrock;
            }

            bool isFlag = (x == redFlagX && y == redFlagY && z == redFlagZ) || (x == blueFlagX && y == blueFlagY && z == blueFlagZ);
            if (z <= Level.ceiling) {
                player.outOfBoundsBlockChanges = 0;
            }

            if (player.team == CTFTeam.Spectator && !player.IsOp && !player.IsVIP) {
                ignore = true;
                player.SendMessage("- &eYou must join a team to build!");
                player.SendBlockchange(x, y, z, mode ? (ushort)0 : oldType);
                return true;
            } else if (!(x < playerX + MAX_DISTANCE
                    && x > playerX - MAX_DISTANCE
                    && y < playerY + MAX_DISTANCE
                    && y > playerY - MAX_DISTANCE
                    && z < playerZ + MAX_DISTANCE
                    && z > playerZ - MAX_DISTANCE)) {
                ignore = true;
            } else if (z > level.ceiling) {
                ignore = true;
                player.SendMessage("- &eYou're not allowed to build this high!");
                player.outOfBoundsBlockChanges++;
                if (player.outOfBoundsBlockChanges == 10) {
                    player.SendMessage("- &cWARNING: You will be kicked automatically if you continue building here.");
                } else if (player.outOfBoundsBlockChanges == 16) {
                    player.Kick("Lag pillaring is not allowed");
                }
                player.SendBlockchange(x, y, z, mode ? (ushort)0 : oldType);
                return true;
            } else if (player.headBlockPosition != null
                    && x == player.headBlockPosition.X
                    && y == player.headBlockPosition.Y
                    && z == player.headBlockPosition.Z) {
                ignore = true;
                player.SendBlockchange(x, y, z, mode ? (ushort)0 : oldType);
                return true;
            } else if (type >= 30 && type <= 33 && mode && !ignore && player.hasTNT) {
                ExplodeTNT(player, Level, player.tntX, player.tntY, player.tntZ, player.tntRadius);
                player.SendBlockchange(x, y, z, oldType);
                player.hasTNT = false;
                player.tntX = 0;
                player.tntY = 0;
                player.tntZ = 0;
                return true;
            } else if (level.IsSolid(x, y, z) && (!player.IsOp || !player.placeSolid)) {
                player.SendBlockchange(x, y, z, oldType);
                return true;
            } else if (Level.IsTNT(x, y, z) && !ignore) {
                player.SendBlockchange(x, y, z, oldType);
                return true;
            } else if (Level.IsMine(x, y, z) && !ignore) {
                player.SendBlockchange(x, y, z, oldType);
                return true;
            } else if (type == Block.TNT && mode && !ignore) {
                if (player.ctfStats.explodes == 0) {
                    player.SendMessage("- &bPlace a purple block to explode the TNT.");
                }
                if (player.team == CTFTeam.Spectator) {
                    player.SendMessage("- &eYou need to join a team to place TNT!");
                    player.SendBlockchange(x, y, z, oldType);
                    return true;
                } else if (mode) {
                    if (!player.hasTNT && !isFlag) {
                        player.hasTNT = true;
                        player.tntX = x;
                        player.tntY = y;
                        player.tntZ = z;
                        Level.Blockchange(x, y, z, type);
                    } else if (!Level.IsTNT(x, y, z) && !isFlag) {
                        player.SendBlockchange(x, y, z, Block.Air);
                    } else if (isFlag) {
                        player.SendBlockchange(x, y, z, oldType);
                    }
                    return true;
                }
            } else if (type == Block.CTF_Mine && mode && !ignore) {
                if (player.team == CTFTeam.Spectator) {
                    player.SendMessage("- &eYou need to join a team to place mines!");
                    player.SendBlockchange(x, y, z, Block.Air);
                } else {
                    if (player.mines.Count < 2 && !isFlag) {
                        Mine mine = new Mine(x, y, z, player.team, player);
                        player.mines.AddLast(mine);
                        player.SendMessage("- Say /d to defuse the mine.");
                        level.Blockchange(x, y, z, player.team == CTFTeam.Red ? Block.CTF_Mine_Red : Block.CTF_Mine_Blue);
                        Level.AddMine(mine);
                        new Thread(delegate () {
                            Thread.Sleep(5000);
                            mine.active = true;
                            player.SendMessage("- &eMine is now active!");
                        }).Start();
                    } else if(!Level.IsMine(x, y, z) && !isFlag) {
                        player.SendBlockchange(x, y, z, Block.Air);
                    } else if (isFlag) {
                        player.SendBlockchange(x, y, z, oldType);
                    }
                }
                return true;
            } else if((type == Block.Lava || type == Block.Water || type == Block.Bedrock
                    || type == Block.CTF_Mine_Red || type == Block.CTF_Mine_Blue) && !player.IsOp) {
                player.SendBlockchange(x, y, z, Block.Air);
                player.SendMessage("- &eYou can't place this block type!");
                return true;
            } else if ((x == redFlagX && y == redFlagY && z == redFlagZ) && mode && !redFlagTaken && !ignore) {
                player.SendBlockchange(x, y, z, Block.CTF_Flag_Red);
                return true;
            } else if ((x == blueFlagX && y == blueFlagY && z == blueFlagZ) && mode && !blueFlagTaken && !ignore) {
                player.SendBlockchange(x, y, z, Block.CTF_Flag_Blue);
                return true;
            }

            if (!mode && !ignore) {
                return ProcessBlockRemove(player, x, y, z);
            }

            return false;
        }

        private bool ProcessBlockRemove(Player p, ushort x, ushort y, ushort z) {
            bool isRedFlag = x == redFlagX && y == redFlagY && z == redFlagZ;
            bool isBlueFlag = x == blueFlagX && y == blueFlagY && z == blueFlagZ;
            bool isRedPlayer = p.team == CTFTeam.Red;
            bool isBluePlayer = p.team == CTFTeam.Blue;
            if (isRedFlag) {
                if (isBluePlayer && !redFlagTaken) {
                    if (RedPlayers == 0 || BluePlayers == 0) {
                        PlaceRedFlag();
                        p.SendMessage("- &eFlag can't be taken when one team has 0 people");
                    } else if(p.duelPlayer != null) {
                        PlaceRedFlag();
                        p.SendMessage("- &eYou can't take the flag while dueling");
                    } else {
                        Chat.MessageGlobal("- &eRed flag taken by {0}&e!", p.ColoredName);
                        p.SendMessage("- &eClick your own flag to capture, or use /fd to drop the flag and pass to a teammate");
                        p.hasFlag = true;
                        redFlagTaken = true;
                        CheckForStalemate();
                        ResetRedFlagPos();
                        if (redFlagDroppedThread == null)
                            redFlagDroppedThread.Interrupt();
                    }
                } else if(isRedPlayer && p.hasFlag && !redFlagTaken && !redFlagDropped) {
                    Chat.MessageGlobal("- &eBlue flag captured by {0} &efor the red team!", p.ColoredName);
                    redCaptures++;
                    p.hasFlag = false;
                    blueFlagTaken = false;
                    PlaceBlueFlag();
                    p.ctfStats.captures++;
                    p.ctfStats.storePoints += 20;
                    if (redCaptures == 5) {
                        nominatedMaps.Clear();
                        EndGame();
                    } else {
                        ShowScore();
                    }
                }
                else if(!redFlagTaken) {
                    PlaceRedFlag();
                }
            } else if(isBlueFlag) {
                if (isRedPlayer && !blueFlagTaken) {
                    if (RedPlayers == 0 || BluePlayers == 0) {
                        PlaceBlueFlag();
                        p.SendMessage("- &eFlag can't be taken when one team has 0 people");
                    } else if (p.duelPlayer != null) {
                        PlaceBlueFlag();
                        p.SendMessage("- &eYou can't take the flag while dueling");
                    } else {
                        Chat.MessageGlobal("- &eBlue flag taken by {0}&e!", p.ColoredName);
                        p.SendMessage("- &eClick your own flag to capture, or use /fd to drop the flag and pass to a teammate");
                        p.hasFlag = true;
                        blueFlagTaken = true;
                        CheckForStalemate();
                        ResetBlueFlagPos();
                        if (blueFlagDroppedThread == null)
                            blueFlagDroppedThread.Interrupt();
                    }
                } else if (isBluePlayer && p.hasFlag && !blueFlagTaken && !blueFlagDropped) {
                    Chat.MessageGlobal("- &eRed flag captured by {0} &efor the blue team!", p.ColoredName);
                    blueCaptures++;
                    p.hasFlag = false;
                    redFlagTaken = false;
                    PlaceRedFlag();
                    p.ctfStats.captures++;
                    p.ctfStats.storePoints += 20;
                    if (blueCaptures == 5) {
                        nominatedMaps.Clear();
                        EndGame();
                    } else {
                        ShowScore();
                    }
                } else if (!blueFlagTaken) {
                    PlaceBlueFlag();
                }
            }
            return false;
        }

        public void ExplodeTNT(Player p, Level level, ushort x, ushort y, ushort z, ushort r) {
            ExplodeTNT(p, level, x, y, z, r, true, false, true, null);
        }

        public void ExplodeTNT(
                Player p,
                Level level,
                ushort x,
                ushort y,
                ushort z,
                ushort r,
                bool lethal,
                bool tk,
                bool deleteSelf,
                string type) {
            if (deleteSelf) {
                level.Blockchange(x, y, z, 0);
            }
            if (p.tntRadius == 3) {
                p.bigTNTRemaining--;
            }
            if (p.bigTNTRemaining <= 0 && p.tntRadius == 3) {
                p.tntRadius = 2;
                p.SendMessage("- &eYour big TNT has expired!");
            }

            int n = 0;
            if (lethal) {
                float px = x + 0.5f, py = y + 0.5f, pz = z + 0.5f;
                float pr = r + 0.5f;
                foreach (Player t in level.players) {
                    float tx = t.Pos.X / 32f;
                    float ty = t.Pos.Y / 32f;
                    float tz = t.Pos.Z / 32f;
                    if (Math.Abs(px - tx) < pr && Math.Abs(py - ty) < pr && Math.Abs(pz - tz) < pr
                            && (p.team != t.team || (tk && (t == p || !t.hasFlag)))
                            && !t.IsSafe() && p.CanKill(t, true)) {
                        t.MarkSafe();
                        n++;
                        Chat.MessageGlobal(
                                "- {0} exploded {1} {2}",
                                p.ColoredName, t.ColoredName, (type == null ? "" : " &f(" + type + ")"));
                        p.OnKill(t);
                        t.SendToTeamSpawn();
                        t.OnKilledBy(p);
                        if (!tk) {
                            CheckFirstBlood(p, t);
                        }
                        if (t.team != CTFTeam.Spectator && t.team != p.team) {
                            p.ctfStats.explodes++;
                            p.ctfStats.storePoints += 5;
                        }
                        if (t.hasFlag) {
                            DropFlag(t.team);
                        }
                    }
                }
            }

            for (ushort cx = (ushort) (x - r); cx <= x + r; cx++) {
                for (ushort cy = (ushort) (y - r); cy <= y + r; cy++) {
                    for (ushort cz = (ushort) (z - r); cz <= z + r; cz++) {
                        BlockID block = level.GetBlock(cx, cy, cz);
                        if (!Level.IsSolid(cx, cy, cz) && block != Block.TNT
                                && !(cx == blueFlagX && cz == blueFlagY && cy == blueFlagZ)
                                && !(cx == redFlagX && cz == redFlagY && cy == redFlagZ)
                                && !Level.IsMine(cx, cy, cz)) {
                            Level.Blockchange(cx, cy, cz, Block.Air);
                        }
                        if (Level.IsMine(cx, cy, cz)) {
                            Mine m = Level.GetMine(cx, cy, cz);
                            if (m.team != p.team) {
                                Level.RemoveMine(m);
                                Level.Blockchange(m.x, m.y, m.z, Block.Air);
                                m.owner.RemoveMine(m);
                                Chat.MessageGlobal("- {0} &edefused {1}&e's mine!", p.ColoredName, m.owner.ColoredName);
                            }
                        }
                    }
                }
            }

            if (n == 2) {
                Chat.MessageGlobal("- &bDouble Kill");
            } else if(n == 3) {
                Chat.MessageGlobal("- &bTriple Kill");
            } else if(n > 3) {
                Chat.MessageGlobal("- &b" + n + "x Kill");
            }
        }
    }
}
