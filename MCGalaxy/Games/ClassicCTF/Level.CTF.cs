using MCGalaxy.Games;
using System;
using System.Collections.Generic;

namespace MCGalaxy {
    public partial class Level {
        public int divider;
        public int ceiling;
        public int floor;
        public int votes = 0;
        private HashSet<Position> solidBlocks = new HashSet<Position>();
        private HashSet<int> solidTypes = new HashSet<int>();
        private bool allSolidTypes = false;
        private readonly LinkedList<Mine> mines = new LinkedList<Mine>();

        public void AddMine(Mine m) {
            mines.AddLast(m);
        }

        public void RemoveMine(Mine m) {
            mines.Remove(m);
        }

        public void ClearMines() {
            mines.Clear();
        }

        public bool IsMine(ushort x, ushort y, ushort z) {
            return GetMine(x, y, z) != null;
        }

        public Mine GetMine(ushort x, ushort y, ushort z) {
            foreach (Mine m in mines) {
                if (m.x == x && m.y == y && m.z == z) {
                    return m;
                }
            }
            return null;
        }

        public LinkedList<Mine> GetMines() {
            return mines;
        }

        public bool IsTNT(ushort x, ushort y, ushort z) {
            foreach (Player p in players) {
                if (p.tntX == x && p.tntY == y && p.tntZ == z)
                    return true;
            }
            return false;
        }

        public void LoadCTFLevel() {
            divider = Config.CTFDivider;
            ceiling = Config.CTFBuildCeiling;
            floor = Config.CTFBuildFloor;

            String solidBlocks = Config.CTFSolidBlocks;
            if (solidBlocks != "") {
                if (solidBlocks == "all") {
                    allSolidTypes = true;
                } else {
                    String[] parts = solidBlocks.Split(' ');
                    foreach (String t in parts) {
                        solidTypes.Add(int.Parse(t));
                    }
                }
            }
        }

        public bool IsSolid(ushort x, ushort y, ushort z) {
            return IsSolid(new Position(x, y, z));
        }

        public bool IsSolid(Position pos) {
            return solidBlocks.Contains(pos);
        }

        public Position GetTeamSpawn(CTFTeam team) {
            if (team == CTFTeam.Spectator) {
                return GetTeamSpawn(new Random().NextDouble() < 0.5 ? CTFTeam.Red : CTFTeam.Blue);
            } else {
                ushort x, y, z;
                if (team == CTFTeam.Red) {
                    x = (ushort) Config.CTFRedSpawnX;
                    y = (ushort)Config.CTFRedSpawnY;
                    z = (ushort)Config.CTFRedSpawnZ;
                } else {
                    x = (ushort)Config.CTFBlueSpawnX;
                    y = (ushort)Config.CTFBlueSpawnY;
                    z = (ushort)Config.CTFBlueSpawnZ;
                }
                return new Position(x * 32 + 16, y * 32 + 16, z * 32 + 16);
            }
        }

        public void DrawFire(Position pos, Orientation rotation) {

        }

        public void ClearFire(Position pos, Orientation rotation) {
        }
    }
}
