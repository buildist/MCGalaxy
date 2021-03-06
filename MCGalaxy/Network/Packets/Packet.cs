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
using MCGalaxy.Blocks;
using MCGalaxy.Maths;

namespace MCGalaxy.Network {

    /// <summary> Constructors for all classic and CPE packets. </summary>
    public static class Packet {
        
        #region Classic
        
        public static byte[] Motd(Player p, string motd) {
            byte[] buffer = new byte[131];
            buffer[0] = Opcode.Handshake;
            buffer[1] = Server.version;
            
            if (motd.Length > NetUtils.StringSize) {
                NetUtils.Write(motd, buffer, 2, p.hasCP437);
                NetUtils.Write(motd.Substring(NetUtils.StringSize), buffer, 66, p.hasCP437);
            } else {
                NetUtils.Write(ServerConfig.Name, buffer, 2, p.hasCP437);
                NetUtils.Write(motd, buffer, 66, p.hasCP437);
            }

            buffer[130] = BlockPerms.UsableBy(p, Block.Bedrock) ? (byte)100 : (byte)0;
            return buffer;
        }
        
        public static byte[] Ping() { return new byte[] { Opcode.Ping }; }
        
        public static byte[] LevelInitalise() { return new byte[] { Opcode.LevelInitialise }; }
        
        public static byte[] LevelFinalise(ushort width, ushort height, ushort length) {
            byte[] buffer = new byte[7];
            buffer[0] = Opcode.LevelFinalise;
            NetUtils.WriteU16(width, buffer, 1);
            NetUtils.WriteU16(height, buffer, 3);
            NetUtils.WriteU16(length, buffer, 5);
            return buffer;
        }
        
        public static byte[] AddEntity(byte entityID, string name, Position pos,
                                       Orientation rot, bool hasCP437, bool extPos) {
            byte[] buffer = new byte[74 + (extPos ? 6 : 0)];
            buffer[0] = Opcode.AddEntity;
            buffer[1] = entityID;
            NetUtils.Write(name.RemoveLastPlus(), buffer, 2, hasCP437);
            
            int offset = NetUtils.WritePos(pos, buffer, 66, extPos);
            buffer[66 + offset] = rot.RotY;
            buffer[67 + offset] = rot.HeadX;
            return buffer;
        }
        
        public static byte[] Teleport(byte entityID, Position pos, Orientation rot, bool extPos) {
            byte[] buffer = new byte[10 + (extPos ? 6 : 0)];
            buffer[0] = Opcode.EntityTeleport;
            buffer[1] = entityID;
            
            int offset = NetUtils.WritePos(pos, buffer, 2, extPos);
            buffer[2 + offset] = rot.RotY;
            buffer[3 + offset] = rot.HeadX;
            return buffer;
        }
        
        public static byte[] RemoveEntity(byte entityID) {
            return new byte[] { Opcode.RemoveEntity, entityID };
        }
        
        public static byte[] BlankMessage() { return Message("", 0, false); }
        
        public static byte[] Message(string message, CpeMessageType type, bool hasCp437) {
            byte[] buffer = new byte[66];
            buffer[0] = Opcode.Message;
            buffer[1] = (byte)type;
            NetUtils.Write(message, buffer, 2, hasCp437);
            return buffer;
        }
        
        public static byte[] Kick(string message, bool cp437) {
            byte[] buffer = new byte[65];
            buffer[0] = Opcode.Kick;
            NetUtils.Write(message, buffer, 1, cp437);
            return buffer;
        }

        public static byte[] UserType(Player p) {
            byte[] buffer = new byte[2];
            buffer[0] = Opcode.SetPermission;
            buffer[1] = BlockPerms.UsableBy(p, Block.Bedrock) ? (byte)100 : (byte)0;
            return buffer;
        }
        
        #endregion
        
        
        #region CPE
        
        public static byte[] ExtInfo(byte extsCount) {
            byte[] buffer = new byte[67];
            buffer[0] = Opcode.CpeExtInfo;
            NetUtils.Write(Server.SoftwareNameVersioned, buffer, 1, false);
            NetUtils.WriteI16((short)extsCount, buffer, 65);
            return buffer;
        }
        
        public static byte[] ExtEntry(string name, int version) {
            byte[] buffer = new byte[69];
            buffer[0] = Opcode.CpeExtEntry;
            NetUtils.Write(name, buffer, 1, false);
            NetUtils.WriteI32(version, buffer, 65);
            return buffer;
        }
        
        public static byte[] ClickDistance(short distance) {
            byte[] buffer = new byte[3];
            buffer[0] = Opcode.CpeSetClickDistance;
            NetUtils.WriteI16(distance, buffer, 1);
            return buffer;
        }
        
        public static byte[] CustomBlockSupportLevel(byte level) {
            return new byte[] { Opcode.CpeCustomBlockSupportLevel, level };
        }
        
        public static byte[] HoldThis(byte block, bool locked) {
            byte[] buffer = new byte[3];
            buffer[0] = Opcode.CpeHoldThis;
            buffer[1] = block;
            buffer[2] = (byte)(locked ? 1 : 0);
            return buffer;
        }
        
        public static byte[] TextHotKey(string label, string input, int keycode,
                                        byte mods, bool hasCP437) {
            byte[] buffer = new byte[134];
            buffer[0] = Opcode.CpeSetTextHotkey;
            NetUtils.Write(label, buffer, 1, hasCP437);
            NetUtils.Write(input, buffer, 65, hasCP437);
            NetUtils.WriteI32(keycode, buffer, 129);
            buffer[133] = mods;
            return buffer;
        }
        
        public static byte[] ExtAddEntity(byte entityID, string name, string displayname, bool hasCP437) {
            byte[] buffer = new byte[130];
            buffer[0] = Opcode.CpeExtAddEntity;
            buffer[1] = entityID;
            NetUtils.Write(name, buffer, 2, hasCP437);
            NetUtils.Write(displayname, buffer, 66, hasCP437);
            return buffer;
        }
        
        public static byte[] ExtAddPlayerName(byte nameID, string listName, string displayName,
                                              string grp, byte grpRank, bool hasCP437) {
            byte[] buffer = new byte[196];
            buffer[0] = Opcode.CpeExtAddPlayerName;
            NetUtils.WriteI16(nameID, buffer, 1);
            NetUtils.Write(listName, buffer, 3, hasCP437);
            NetUtils.Write(displayName, buffer, 67, hasCP437);
            NetUtils.Write(grp, buffer, 131, hasCP437);
            buffer[195] = grpRank;
            return buffer;
        }
        
        public static byte[] ExtRemovePlayerName(byte nameID) {
            byte[] buffer = new byte[3];
            buffer[0] = Opcode.CpeExtRemovePlayerName;
            NetUtils.WriteI16(nameID, buffer, 1);
            return buffer;
        }

        public static byte[] EnvColor(byte type, short r, short g, short b) {
            byte[] buffer = new byte[8];
            buffer[0] = Opcode.CpeEnvColors;
            buffer[1] = type;
            NetUtils.WriteI16(r, buffer, 2);
            NetUtils.WriteI16(g, buffer, 4);
            NetUtils.WriteI16(b, buffer, 6);
            return buffer;
        }
        
        public static byte[] MakeSelection(byte selID, string label, Vec3U16 p1, Vec3U16 p2,
                                           short r, short g, short b, short opacity, bool hasCP437) {
            byte[] buffer = new byte[86];
            buffer[0] = Opcode.CpeMakeSelection;
            buffer[1] = selID;
            NetUtils.Write(label, buffer, 2, hasCP437);
            
            NetUtils.WriteU16(p1.X, buffer, 66);
            NetUtils.WriteU16(p1.Y, buffer, 68);
            NetUtils.WriteU16(p1.Z, buffer, 70);
            NetUtils.WriteU16(p2.X, buffer, 72);
            NetUtils.WriteU16(p2.Y, buffer, 74);
            NetUtils.WriteU16(p2.Z, buffer, 76);
            
            NetUtils.WriteI16(r, buffer, 78);
            NetUtils.WriteI16(g, buffer, 80);
            NetUtils.WriteI16(b, buffer, 82);
            NetUtils.WriteI16(opacity, buffer, 84);
            return buffer;
        }
        
        public static byte[] DeleteSelection(byte selID) {
            byte[] buffer = new byte[2];
            buffer[0] = Opcode.CpeRemoveSelection;
            buffer[1] = selID;
            return buffer;
        }
        
        public static byte[] BlockPermission(byte block, bool place, bool delete) {
            byte[] buffer = new byte[4];
            WriteBlockPermission(block, place, delete, buffer, 0);
            return buffer;
        }
        
        public static void WriteBlockPermission(byte block, bool place, bool delete, byte[] buffer, int index) {
            buffer[index + 0] = Opcode.CpeSetBlockPermission;
            buffer[index + 1] = block;
            buffer[index + 2] = place ? (byte)1 : (byte)0;
            buffer[index + 3] = delete ? (byte)1 : (byte)0;
        }
        
        public static byte[] ChangeModel(byte entityID, string model, bool hasCP437) {
            byte[] buffer = new byte[66];
            buffer[0] = Opcode.CpeChangeModel;
            buffer[1] = entityID;
            NetUtils.Write(model, buffer, 2, hasCP437);
            return buffer;
        }
        
        public static byte[] MapAppearance(string url, byte side, byte edge, int sideLevel,
                                           bool hasCP437) {
            byte[] buffer = new byte[69];
            WriteMapAppearance(buffer, url, side, edge, sideLevel, hasCP437);
            return buffer;
        }
        
        public static byte[] MapAppearanceV2(string url, byte side, byte edge, int sideLevel,
                                             int cloudHeight, int maxFog, bool hasCP437) {
            byte[] buffer = new byte[73];
            WriteMapAppearance(buffer, url, side, edge, sideLevel, hasCP437);
            NetUtils.WriteI16((short)cloudHeight, buffer, 69);
            NetUtils.WriteI16((short)maxFog, buffer, 71);
            return buffer;
        }
        
        static void WriteMapAppearance(byte[] buffer, string url, byte side, byte edge,
                                       int sideLevel, bool hasCP437) {
            buffer[0] = Opcode.CpeEnvSetMapApperance;
            NetUtils.Write(url, buffer, 1, hasCP437);
            buffer[65] = side;
            buffer[66] = edge;
            NetUtils.WriteI16((short)sideLevel, buffer, 67);
        }

        public static byte[] EnvWeatherType(byte type) { // 0 - sunny; 1 - raining; 2 - snowing
            byte[] buffer = new byte[2];
            buffer[0] = Opcode.CpeEnvWeatherType;
            buffer[1] = type;
            return buffer;
        }
        
        public static byte[] HackControl(bool canFly, bool canNoclip,
                                         bool canSpeed, bool canRespawn,
                                         bool can3rdPerson, short maxJumpHeight) {
            byte[] buffer = new byte[8];
            buffer[0] = Opcode.CpeHackControl;
            buffer[1] = (byte)(canFly ? 1 : 0);
            buffer[2] = (byte)(canNoclip ? 1 : 0);
            buffer[3] = (byte)(canSpeed ? 1 : 0);
            buffer[4] = (byte)(canRespawn ? 1 : 0);
            buffer[5] = (byte)(can3rdPerson ? 1 : 0);
            NetUtils.WriteI16(maxJumpHeight, buffer, 6);
            return buffer;
        }
        
        public static byte[] ExtAddEntity2(byte entityID, string skinName, string displayName,
                                           Position pos, Orientation rot, bool hasCP437, bool extPos) {
            byte[] buffer = new byte[138 + (extPos ? 6 : 0)];
            buffer[0] = Opcode.CpeExtAddEntity2;
            buffer[1] = entityID;
            NetUtils.Write(displayName.RemoveLastPlus(), buffer, 2, hasCP437);
            NetUtils.Write(skinName.RemoveLastPlus(), buffer, 66, hasCP437);
            
            int offset = NetUtils.WritePos(pos, buffer, 130, extPos);
            buffer[130 + offset] = rot.RotY;
            buffer[131 + offset] = rot.HeadX;
            return buffer;
        }
        
        public static byte[] SetTextColor(ColorDesc col) {
            byte[] buffer = new byte[6];
            buffer[0] = Opcode.CpeSetTextColor;
            buffer[1] = col.R; buffer[2] = col.G; buffer[3] = col.B; buffer[4] = col.A;
            buffer[5] = col.Index;
            return buffer;
        }

        
        public static byte[] EnvMapUrl(string url, bool hasCP437) {
            byte[] buffer = new byte[65];
            buffer[0] = Opcode.CpeSetMapEnvUrl;
            NetUtils.Write(url, buffer, 1, hasCP437);
            return buffer;
        }
        
        public static byte[] EnvMapProperty(EnvProp prop, int value) {
            byte[] buffer = new byte[6];
            buffer[0] = Opcode.CpeSetMapEnvProperty;
            buffer[1] = (byte)prop;
            NetUtils.WriteI32(value, buffer, 2);
            return buffer;
        }
        
        public static byte[] EntityProperty(byte entityID, EntityProp prop, int value) {
            byte[] buffer = new byte[7];
            buffer[0] = Opcode.CpeSetEntityProperty;
            buffer[1] = entityID;
            buffer[2] = (byte)prop;
            NetUtils.WriteI32(value, buffer, 3);
            return buffer;
        }
        
        public static byte[] TwoWayPing(bool serverToClient, ushort data) {
            byte[] buffer = new byte[4];
            buffer[0] = Opcode.CpeTwoWayPing;
            buffer[1] = (byte)(serverToClient ? 1 : 0);
            NetUtils.WriteU16(data, buffer, 2);
            return buffer;
        }
        
        public static byte[] SetInventoryOrder(byte block, byte position) {
            return new byte[] { Opcode.CpeSetInventoryOrder, block, position };
        }
        
        #endregion
        
        
        #region Block definitions
        
        public static byte[] DefineBlock(BlockDefinition def, bool hasCP437) {
            byte[] buffer = new byte[80];
            int i = 0;
            buffer[i++] = Opcode.CpeDefineBlock;
            MakeDefineBlockStart(def, buffer, ref i, false, hasCP437);
            buffer[i++] = def.Shape;
            MakeDefineBlockEnd(def, ref i, buffer);
            return buffer;
        }
        
        public static byte[] UndefineBlock(byte block) {
            return new byte[] { Opcode.CpeRemoveBlockDefinition, block };
        }
        
        public static byte[] DefineBlockExt(BlockDefinition def, bool uniqueSideTexs, bool hasCP437) {
            byte[] buffer = new byte[uniqueSideTexs ? 88 : 85];
            int i = 0;
            buffer[i++] = Opcode.CpeDefineBlockExt;
            MakeDefineBlockStart(def, buffer, ref i, uniqueSideTexs, hasCP437);
            buffer[i++] = def.MinX; buffer[i++] = def.MinZ; buffer[i++] = def.MinY;
            buffer[i++] = def.MaxX; buffer[i++] = def.MaxZ; buffer[i++] = def.MaxY;
            MakeDefineBlockEnd(def, ref i, buffer);
            return buffer;
        }
        
        static void MakeDefineBlockStart(BlockDefinition def, byte[] buffer, ref int i,
                                         bool uniqueSideTexs, bool hasCP437) {
            // speed = 2^((raw - 128) / 64);
            // therefore raw = 64log2(speed) + 128
            byte rawSpeed = (byte)(64 * Math.Log(def.Speed, 2) + 128);
            buffer[i++] = def.BlockID;
            NetUtils.Write(def.Name, buffer, i, hasCP437);
            i += NetUtils.StringSize;
            buffer[i++] = def.CollideType;
            buffer[i++] = rawSpeed;
            
            buffer[i++] = def.TopTex;
            if (uniqueSideTexs) {
                buffer[i++] = def.LeftTex;  buffer[i++] = def.RightTex;
                buffer[i++] = def.FrontTex; buffer[i++] = def.BackTex;
            } else {
                buffer[i++] = def.SideTex;
            }
            
            buffer[i++] = def.BottomTex;
            buffer[i++] = (byte)(def.BlocksLight ? 0 : 1);
            buffer[i++] = def.WalkSound;
            buffer[i++] = (byte)(def.FullBright ? 1 : 0);
        }
        
        static void MakeDefineBlockEnd(BlockDefinition def, ref int i, byte[] buffer) {
            buffer[i++] = def.BlockDraw;
            buffer[i++] = def.FogDensity;
            buffer[i++] = def.FogR; buffer[i++] = def.FogG; buffer[i++] = def.FogB;
        }
        #endregion
    }
}
