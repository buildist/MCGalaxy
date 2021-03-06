﻿/*
    Copyright 2011 MCForge
    
    Written by fenderrock87
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
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
using MCGalaxy.Config;
using BlockID = System.UInt16;

namespace MCGalaxy.Games {
    
    public sealed class CTFConfig {
        
        [ConfigInt("base.red.x", null, 0)]
        public int RedFlagX;
        [ConfigInt("base.red.y", null, 0)]
        public int RedFlagY;
        [ConfigInt("base.red.z", null, 0)]
        public int RedFlagZ;
        [ConfigBlock("base.red.block", null, Block.Air)]
        public BlockID RedFlagBlock;
        
        [ConfigInt("base.blue.x", null, 0)]
        public int BlueFlagX;
        [ConfigInt("base.blue.y", null, 0)]
        public int BlueFlagY;
        [ConfigInt("base.blue.z", null, 0)]
        public int BlueFlagZ;
        [ConfigBlock("base.blue.block", null, Block.Air)]
        public BlockID BlueFlagBlock;
        
        [ConfigInt("base.red.spawnx", null, 0)]
        public int RedSpawnX;
        [ConfigInt("base.red.spawny", null, 0)]
        public int RedSpawnY;
        [ConfigInt("base.red.spawnz", null, 0)]
        public int RedSpawnZ;
        
        [ConfigInt("base.blue.spawnx", null, 0)]
        public int BlueSpawnX;
        [ConfigInt("base.blue.spawny", null, 0)]
        public int BlueSpawnY;
        [ConfigInt("base.blue.spawnz", null, 0)]
        public int BlueSpawnZ;
        
        [ConfigInt("map.line.z", null, 0)]
        public int ZDivider;
        [ConfigInt("game.maxpoints", null, 0)]
        public int RoundPoints;
        [ConfigInt("game.tag.points-gain", null, 0)]
        public int Tag_PointsGained;
        [ConfigInt("game.tag.points-lose", null, 0)]
        public int Tag_PointsLost;
        [ConfigInt("game.capture.points-gain", null, 0)]
        public int Capture_PointsGained;
        [ConfigInt("game.capture.points-lose", null, 0)]
        public int Capture_PointsLost;
        
        
        /// <summary> Sets the default CTF config values for the given map. </summary>
        public void SetDefaults(Level map) {
            ZDivider = map.Length / 2;
            RedFlagBlock = Block.Red;
            BlueFlagBlock = Block.Blue;
            int midX = map.Width / 2, maxZ = map.Length - 1;
            
            RedFlagX = midX; RedSpawnX = midX * 32;
            RedFlagY = 6;    RedSpawnY = 4 * 32 + Entities.CharacterHeight;
            RedFlagZ = 0;    RedSpawnZ = 0 * 32;
            
            BlueFlagX = midX; BlueSpawnX = midX * 32;
            BlueFlagY = 6;    BlueSpawnY = 4 * 32 + Entities.CharacterHeight;
            BlueFlagZ = maxZ; BlueSpawnZ = maxZ * 32;
            
            RoundPoints = 3;
            Tag_PointsGained = 5;
            Tag_PointsLost = 5;
            Capture_PointsGained = 10;
            Capture_PointsLost = 10;
        }
        
        
        static ConfigElement[] elems;
        public void Retrieve(string mapName) {
            if (elems == null) elems = ConfigElement.GetAll(typeof(CTFConfig));
            PropertiesFile.Read("CTF/" + mapName + ".config", LineProcessor);
        }
        
        public void Save(string mapName) {
            ConfigElement[] elements = elems;
            using (StreamWriter w = new StreamWriter("CTF/" + mapName + ".config")) {
                for (int i = 0; i < elements.Length; i++) {
                    w.WriteLine(elements[i].Format(this));
                }
            }
        }
        
        void LineProcessor(string key, string value) {
            if (!ConfigElement.Parse(elems, key, value, this)) {
                Logger.Log(LogType.Warning, "\"{0}\" was not a recognised CTF config key.", key);
            }
        }
    }
}
