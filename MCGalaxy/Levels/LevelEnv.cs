﻿/*
    Copyright 2015 MCGalaxy team
    
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
using MCGalaxy.Commands;
using MCGalaxy.Commands.Building;
using MCGalaxy.Network;
using MCGalaxy.Blocks;
using BlockID = System.UInt16;
using BlockRaw = System.Byte;

namespace MCGalaxy {
    public static class LevelEnv {
        
        static bool IsResetString(string value) {
            return value.CaselessEq("normal") || value.CaselessEq("reset") || value.CaselessEq("default");
        }
        
        public static void SetWeather(Player p, string input, string area, ref int target) {
            byte value;
            if (IsResetString(input)) {
                Player.Message(p, "Reset weather for {0} %Sto 0 (%SSun)", area);
                value = 0;
            } else {
                if (byte.TryParse(input, out value)) {
                } else if (input.CaselessEq("sun")) { value = 0;
                } else if (input.CaselessEq("rain")) { value = 1;
                } else if (input.CaselessEq("snow")) { value = 2;
                }
                
                if (value > 2) {
                    Player.Message(p, "Weather can be either sun, rain, or snow."); return;
                }
                string type = value == 0 ? "%SSun" : (value == 1 ? "&1Rain" : "&fSnow");
                Player.Message(p, "Set weather for {0} %Sto {1} ({2}%S)", area, value, type);
            }            
            target = value;
        }
        
        public static void SetBlock(Player p, string input, string area,
                                    string variable, byte defValue, ref BlockID target) {
            if (IsResetString(input)) {
                Player.Message(p, "Reset {0} for {1} %Sto normal", variable, area);
                target = defValue;
            } else {
                BlockID block;
                if (!CommandParser.GetBlock(p, input, out block)) return;
                if (Block.IsPhysicsType(block)) {
                    Player.Message(p, "Cannot use physics block ids for /env."); return;
                }
                
                string name = Block.GetName(p, block);
                target = block;
                Player.Message(p, "Set {0} for {1} %Sto {2}", variable, area, name);
            }
        }
        
        public static void SetShort(Player p, string input, string area,
                                    string variable, int defValue, ref int target) {
            if (IsResetString(input)) {
                Player.Message(p, "Reset {0} for {1} %Sto normal", variable, area);
                target = (short)defValue;
            } else {
                int value = 0;
                if (!CommandParser.GetInt(p, input, variable, ref value, 
                                          short.MinValue, short.MaxValue)) return;
                
                target = (short)value;
                Player.Message(p, "Set {0} for {1} %Sto {2}", variable, area, value);
            }
        }
        
        public static void SetFloat(Player p, string input, string area, int scale, string variable,
                                    int defValue, ref int target, int min, int max) {
            if (IsResetString(input)) {
                Player.Message(p, "Reset {0} for {1} %Sto normal", variable, area);
                target = defValue;
            } else {
                float value = 0, minF = (float)min / scale, maxF = (float)max / scale;
                if (!CommandParser.GetReal(p, input, variable, ref value, minF, maxF)) return;

                target = (int)(value * scale);
                Player.Message(p, "Set {0} for {1} %Sto {2}", variable, area, value.ToString("F4"));
            }
        }
        
        public static void SetColor(Player p, string input, string area,
                                    string variable, ref string target) {
            if (IsResetString(input)) {
                Player.Message(p, "Reset {0} for {1} %Sto normal", variable, area);
                target = "";
            } else {
                ColorDesc rgb = default(ColorDesc);
                if (!CommandParser.GetHex(p, input, ref rgb)) return;
                
                Player.Message(p, "Set {0} for {1} %Sto #{2}", variable, area, input);
                target = Utils.Hex(rgb.R, rgb.G, rgb.B);
            }
        }
        
        public static void SetBool(Player p, string value, string area,
                                   string variable, bool defValue, ref bool target) {
            if (IsResetString(value)) {
                Player.Message(p, "Reset {0} for {1} %Sto normal", variable, area);
                target = defValue;
            } else {
                if (!CommandParser.GetBool(p, value, ref target)) return;
                
                Player.Message(p, "Set {0} for {1} %Sto {2}", variable,
                               area, target ? "&aON" : "&cOFF");
            }
        }

        
        internal static void UpdateWeather(Level lvl, byte weather) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level != lvl || !pl.Supports(CpeExt.EnvWeatherType)) continue;
                pl.Send(Packet.EnvWeatherType(weather));
            }
        }
        
        internal static void UpdateAppearance(Level lvl, EnvProp prop, int value) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level != lvl) continue;
                
                if (pl.Supports(CpeExt.EnvMapAspect)) {
                    pl.Send(Packet.EnvMapProperty(prop, value));
                } else {
                    pl.SendCurrentMapAppearance();
                }
            }
        }
        
        internal static void UpdateColor(Level lvl, byte type, string hex) {
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level != lvl || !pl.Supports(CpeExt.EnvColors)) continue;
                pl.SendEnvColor(type, hex);
            }
        }
    }
}