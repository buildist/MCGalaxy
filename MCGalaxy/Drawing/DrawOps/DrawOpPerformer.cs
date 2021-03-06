﻿/*
    Copyright 2015 MCGalaxy
        
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
using MCGalaxy.Blocks.Physics;
using MCGalaxy.Commands;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Network;
using MCGalaxy.Undo;
using MCGalaxy.Maths;
using BlockRaw = System.Byte;

namespace MCGalaxy.Drawing {
    internal struct PendingDrawOp {
        public DrawOp Op;
        public Brush Brush;
        public Vec3S32[] Marks;
    }
}

namespace MCGalaxy.Drawing.Ops {
    
    public static class DrawOpPerformer {
        
        public static Level Setup(DrawOp op, Player p, Vec3S32[] marks) {
            op.SetMarks(marks);
            Level lvl = p == null ? null : p.level;
            op.SetLevel(lvl);
            op.Player = p;
            return lvl;
        }
        
        public static bool Do(DrawOp op, Brush brush, Player p,
                              Vec3S32[] marks, bool checkLimit = true) {
            Level lvl = Setup(op, p, marks);
            
            if (lvl != null && !lvl.Config.DrawingAllowed) {
                Player.Message(p, "Drawing commands are turned off on this map.");
                return false;
            }
            if (lvl != null && !lvl.BuildAccess.CheckDetailed(p)) {
                Player.Message(p, "Hence you cannot use draw commands on this map.");
                return false;
            }
            
            long affected = op.BlocksAffected(lvl, marks);
            if (p != null && op.AffectedByTransform)
                p.Transform.GetBlocksAffected(ref affected);
            if (checkLimit && !op.CanDraw(marks, p, affected)) return false;
            
            if (brush != null && affected != -1) {
                const string format = "{0}({1}): affecting up to {2} blocks";
                if (p == null || !p.Ignores.DrawOutput) {
                    Player.Message(p, format, op.Name, brush.Name, affected);
                }
            } else if (affected != -1) {
                const string format = "{0}: affecting up to {1} blocks";
                if (p == null || !p.Ignores.DrawOutput) {
                    Player.Message(p, format, op.Name, affected);
                }
            }
            
            DoQueuedDrawOp(p, op, brush, marks);
            return true;
        }
        
        internal static void DoQueuedDrawOp(Player p, DrawOp op, Brush brush, Vec3S32[] marks) {
            PendingDrawOp item = new PendingDrawOp();
            item.Op = op; item.Brush = brush; item.Marks = marks;

            lock (p.pendingDrawOpsLock) {
                p.PendingDrawOps.Add(item);
                // Another thread is already processing draw ops.
                if (p.PendingDrawOps.Count > 1) return;
            }
            ProcessDrawOps(p);
        }
        
        static void ProcessDrawOps(Player p) {
            while (true) {
                PendingDrawOp item;
                lock (p.pendingDrawOpsLock) {
                    if (p.PendingDrawOps.Count == 0) return;
                    item = p.PendingDrawOps[0];
                    p.PendingDrawOps.RemoveAt(0);
                    
                    // Flush any remaining draw ops if the player has left the server.
                    // (so as to not keep alive references)
                    if (p.disconnected) {
                        p.PendingDrawOps.Clear();
                        return;
                    }
                }
                
                UndoDrawOpEntry entry = new UndoDrawOpEntry();
                entry.DrawOpName = item.Op.Name;
                entry.LevelName = item.Op.Level.name;
                entry.Start = DateTime.UtcNow;
                // Use same time method as DoBlockchange writing to undo buffer
                int timeDelta = (int)DateTime.UtcNow.Subtract(Server.StartTime).TotalSeconds;
                entry.Start = Server.StartTime.AddTicks(timeDelta * TimeSpan.TicksPerSecond);
                
                if (item.Brush != null) item.Brush.Configure(item.Op, p);
                bool needReload = DoDrawOp(item, p);
                timeDelta = (int)DateTime.UtcNow.Subtract(Server.StartTime).TotalSeconds + 1;
                entry.End = Server.StartTime.AddTicks(timeDelta * TimeSpan.TicksPerSecond);
                
                if (item.Op.Undoable) p.DrawOps.Add(entry);
                if (p.DrawOps.Count > 200) p.DrawOps.RemoveFirst();
                
                if (needReload) DoReload(p, item.Op.Level);
                item.Op.TotalModified = 0; // reset total modified (as drawop instances are reused in static mode)
            }
        }
        
        static bool DoDrawOp(PendingDrawOp item, Player p) {
            Level lvl = item.Op.Level;
            DrawOpOutputter outputter = new DrawOpOutputter(item.Op);
            
            if (item.Op.AffectedByTransform) {
                p.Transform.Perform(item.Marks, p, lvl, item.Op, item.Brush, outputter.Output);
            } else {
                item.Op.Perform(item.Marks, item.Brush, outputter.Output);
            }
            return item.Op.TotalModified >= outputter.reloadThreshold;
        }

        static void DoReload(Player p, Level lvl) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level.name.CaselessEq(lvl.name))
                    LevelActions.ReloadMap(p, pl, true);
            }
            Server.DoGC();
        }

        
        class DrawOpOutputter {
            readonly DrawOp op;
            internal readonly int reloadThreshold;
            
            public DrawOpOutputter(DrawOp op) { 
                this.op = op;
                reloadThreshold = op.Level.ReloadThreshold;
            }
            
            public void Output(DrawOpBlock b) {
                if (b.Block == Block.Invalid) return;
                Level lvl = op.Level;
                Player p = op.Player;
                if (b.X >= lvl.Width || b.Y >= lvl.Height || b.Z >= lvl.Length) return;
                
                int index = b.X + lvl.Width * (b.Z + b.Y * lvl.Length);
                ushort old = lvl.blocks[index];
                if (old == Block.custom_block) old = (ushort)(Block.Extended | lvl.GetExtTileNoCheck(b.X, b.Y, b.Z));
                if (old == Block.Invalid) return;
                
                // Check to make sure the block is actually different and that we can change it
                if (old == b.Block || !lvl.CheckAffectPermissions(p, b.X, b.Y, b.Z, old, b.Block)) return;
                
                // Set the block (inlined)
                lvl.blocks[index] = b.Block >= Block.Extended ? Block.custom_block : (BlockRaw)b.Block;
                lvl.Changed = true;
                if (b.Block >= Block.Extended) {
                    lvl.SetExtTileNoCheck(b.X, b.Y, b.Z, (BlockRaw)b.Block);
                } else if (old >= Block.Extended) {
                    lvl.RevertExtTileNoCheck(b.X, b.Y, b.Z);
                }
                
                if (p != null) {
                    lvl.BlockDB.Cache.Add(p, b.X, b.Y, b.Z, op.Flags, old, b.Block);
                    p.SessionModified++; p.TotalModified++; p.TotalDrawn++; // increment block stats inline
                }
                
                // Potentially buffer the block change
                if (op.TotalModified == reloadThreshold) {
                    if (p == null || !p.Ignores.DrawOutput) {
                        Player.Message(p, "Changed over {0} blocks, preparing to reload map..", reloadThreshold);
                    }

                    lock (lvl.queueLock)
                        lvl.blockqueue.Clear();
                } else if (op.TotalModified < reloadThreshold) {
                    if (!Block.VisuallyEquals(old, b.Block)) BlockQueue.Add(p, index, b.Block);

                    if (lvl.physics > 0) {
                        if (old == Block.Sponge && b.Block != Block.Sponge)
                            OtherPhysics.DoSpongeRemoved(lvl, index, false);
                        if (old == Block.LavaSponge && b.Block != Block.LavaSponge)
                            OtherPhysics.DoSpongeRemoved(lvl, index, true);

                        if (lvl.ActivatesPhysics(b.Block)) lvl.AddCheck(index);
                    }
                }
                op.TotalModified++;
                
                
                // Attempt to prevent the BlockDB from growing too large (> 1,000,000 entries)
                int count = lvl.BlockDB.Cache.Count;
                if (count == 0 || (count % 1000000) != 0) return;
                
                // if drawop has a read lock on BlockDB (e.g. undo/redo), we must release it here
                bool hasReadLock = false;
                if (op.BlockDBReadLock != null) {
                    op.BlockDBReadLock.Dispose();
                    hasReadLock = true;
                }
                
                using (IDisposable wLock = lvl.BlockDB.Locker.AccquireWrite(100)) {
                    if (wLock != null) lvl.BlockDB.WriteEntries();
                }
                
                if (!hasReadLock) return;
                op.BlockDBReadLock = lvl.BlockDB.Locker.AccquireRead();
            }
        }
    }
}
