﻿/*
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
using System.IO;
using MCGalaxy.Bots;
using MCGalaxy.DB;
using MCGalaxy.Events;
using MCGalaxy.SQL;
using MCGalaxy.Util;

namespace MCGalaxy {
    
    public static class LevelActions {
        
        /// <summary> Renames the .lvl (and related) files and database tables. Does not unload. </summary>
        public static void Rename(string src, string dst) {
            File.Move(LevelInfo.MapPath(src), LevelInfo.MapPath(dst));
            
            MoveIfExists(LevelInfo.MapPath(src) + ".backup",
                         LevelInfo.MapPath(dst) + ".backup");
            MoveIfExists("levels/level properties/" + src + ".properties",
                         "levels/level properties/" + dst + ".properties");
            MoveIfExists("levels/level properties/" + src,
                         "levels/level properties/" + dst + ".properties");
            MoveIfExists("blockdefs/lvl_" + src + ".json",
                         "blockdefs/lvl_" + dst + ".json");
            MoveIfExists("blockprops/lvl_" + src + ".txt",
                         "blockprops/lvl_" + dst + ".txt");
            MoveIfExists(BotsFile.BotsPath(src),
                         BotsFile.BotsPath(dst));
            
            // TODO: Should we move backups still
            try {
                //MoveBackups(src, dst);
            } catch {
            }
            
            RenameDatabaseTables(src, dst);
            BlockDBFile.MoveBackingFile(src, dst);
        }
        
        static void RenameDatabaseTables(string src, string dst) {
            if (Database.Backend.TableExists("Block" + src))
                Database.Backend.RenameTable("Block" + src, "Block" + dst);
            object srcLocker = ThreadSafeCache.DBCache.GetLocker(src);
            object dstLockder = ThreadSafeCache.DBCache.GetLocker(dst);
            
            lock (srcLocker)
                lock (dstLockder)
            {
                if (Database.TableExists("Portals" + src)) {
                    Database.Backend.RenameTable("Portals" + src, "Portals" + dst);
                    Database.Backend.UpdateRows("Portals" + dst, "ExitMap = @1",
                                                "WHERE ExitMap = @0", src, dst);
                }
                
                if (Database.TableExists("Messages" + src)) {
                    Database.Backend.RenameTable("Messages" + src, "Messages" + dst);
                }
                if (Database.TableExists("Zone" + src)) {
                    Database.Backend.RenameTable("Zone" + src, "Zone" + dst);
                }
            }
        }
        
        static void MoveIfExists(string src, string dst) {
            if (!File.Exists(src)) return;
            try {
                File.Move(src, dst);
            } catch (Exception ex) {
                Logger.LogError(ex);
            }
        }
        
        /*static void MoveBackups(string src, string dst) {
            string srcBase = LevelInfo.BackupBasePath(src);
            string dstBase = LevelInfo.BackupBasePath(dst);
            if (!Directory.Exists(srcBase)) return;
            Directory.CreateDirectory(dstBase);
            
            string[] backups = Directory.GetDirectories(srcBase);
            for (int i = 0; i < backups.Length; i++) {
                string name = LevelInfo.BackupNameFrom(backups[i]);
                string srcFile = LevelInfo.BackupFilePath(src, name);
                string dstFile = LevelInfo.BackupFilePath(dst, name);                
                string dstDir = LevelInfo.BackupDirPath(dst, name);
                
                Directory.CreateDirectory(dstDir);
                File.Move(srcFile, dstFile);
                Directory.Delete(backups[i]);
            }
            Directory.Delete(srcBase);
        }*/
        
        
        public const string DeleteFailedMessage = "Unable to delete the level, because it could not be unloaded. A game may currently be running on it.";
        /// <summary> Deletes the .lvl (and related) files and database tables. Unloads level if it is loaded. </summary>
        public static bool Delete(string name) {
            Level lvl = LevelInfo.FindExact(name);
            if (lvl != null && !lvl.Unload()) return false;
            
            if (!Directory.Exists("levels/deleted"))
                Directory.CreateDirectory("levels/deleted");
            
            if (File.Exists(LevelInfo.DeletedPath(name))) {
                int num = 0;
                while (File.Exists(LevelInfo.DeletedPath(name + num))) num++;

                File.Move(LevelInfo.MapPath(name), LevelInfo.DeletedPath(name + num));
            } else {
                File.Move(LevelInfo.MapPath(name), LevelInfo.DeletedPath(name));
            }

            DeleteIfExists(LevelInfo.MapPath(name) + ".backup");
            DeleteIfExists("levels/level properties/" + name);
            DeleteIfExists("levels/level properties/" + name + ".properties");
            DeleteIfExists("blockdefs/lvl_" + name + ".json");
            DeleteIfExists("blockprops/lvl_" + name + ".txt");
            DeleteIfExists(BotsFile.BotsPath(name));
            
            DeleteDatabaseTables(name);
            BlockDBFile.DeleteBackingFile(name);
            return true;
        }
        
        static void DeleteDatabaseTables(string name) {
            if (Database.Backend.TableExists("Block" + name))
                Database.Backend.DeleteTable("Block" + name);
            
            object locker = ThreadSafeCache.DBCache.GetLocker(name);
            lock (locker) {
                if (Database.TableExists("Portals" + name)) {
                    Database.Backend.DeleteTable("Portals" + name);
                }
                if (Database.TableExists("Messages" + name)) {
                    Database.Backend.DeleteTable("Messages" + name);
                }
                if (Database.TableExists("Zone" + name)) {
                    Database.Backend.DeleteTable("Zone" + name);
                }
            }
        }

        static void DeleteIfExists(string src) {
            if (!File.Exists(src)) return;
            try {
                File.Delete(src);
            } catch (Exception ex) {
                Logger.LogError(ex);
            }
        }
        
        
        public static void Replace(Level old, Level lvl) {
            LevelDB.SaveBlockDB(old);
            LevelInfo.Remove(old);        
            LevelInfo.Add(lvl);
            
            old.SetPhysics(0);
            old.ClearPhysics();
            lvl.StartPhysics();
            
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level != old) continue;
                pl.level = lvl;
                ReloadMap(null, pl, false);
            }
            
            old.Unload(true, false);
            if (old == Server.mainLevel)
                Server.mainLevel = lvl;
        }
        
        public static void ReloadMap(Player p, Player who, bool showMessage) {
            who.Loading = true;
            Entities.DespawnEntities(who);
            who.SendMap(who.level);
            Entities.SpawnEntities(who);
            who.Loading = false;
            if (!showMessage) return;
            
            if (p == null || !Entities.CanSee(who, p)) {
                who.SendMessage("&bMap reloaded");
            } else {
                who.SendMessage("&bMap reloaded by " + p.ColoredName);
            }
            if (Entities.CanSee(p, who)) {
                Player.Message(p, "&4Finished reloading for " + who.ColoredName);
            }
        }
        
        
        public static void CopyLevel(string src, string dst) {
            File.Copy(LevelInfo.MapPath(src), LevelInfo.MapPath(dst));
            
            CopyIfExists("levels/level properties/" + src,
                         "levels/level properties/" + dst + ".properties");
            CopyIfExists("levels/level properties/" + src + ".properties",
                         "levels/level properties/" + dst + ".properties");
            CopyIfExists("blockdefs/lvl_" + src + ".json",
                         "blockdefs/lvl_" + dst + ".json");
            CopyIfExists("blockprops/lvl_" + src + ".txt",
                         "blockprops/lvl_" + dst + ".txt");
            CopyIfExists(BotsFile.BotsPath(src),
                         BotsFile.BotsPath(dst));
            
            CopyDatabaseTables(src, dst);
        }
        
        static void CopyDatabaseTables(string src, string dst) {
            object srcLocker = ThreadSafeCache.DBCache.GetLocker(src);
            object dstLockder = ThreadSafeCache.DBCache.GetLocker(dst);
            
            lock (srcLocker)
                lock (dstLockder)
            {
                if (Database.TableExists("Portals" + src)) {
                    Database.Backend.CreateTable("Portals" + dst, LevelDB.createPortals);
                    Database.Backend.CopyAllRows("Portals" + src, "Portals" + dst);
                    Database.Backend.UpdateRows("Portals" + dst, "ExitMap = @1",
                                                "WHERE ExitMap = @0", src, dst);
                }
                
                if (Database.TableExists("Messages" + src)) {
                    Database.Backend.CreateTable("Messages" + dst, LevelDB.createMessages);
                    Database.Backend.CopyAllRows("Messages" + src, "Messages" + dst);
                }
            }
        }
        
        static void CopyIfExists(string src, string dst) {
            if (!File.Exists(src)) return;
            try {
                File.Copy(src, dst, true);
            } catch (Exception ex) {
                Logger.LogError(ex);
            }
        }
    }
}
