﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
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
using System.IO;
using MCGalaxy.Config;
using MCGalaxy.Games;
using BlockID = System.UInt16;

namespace MCGalaxy {
    public abstract class AreaConfig {
        [ConfigString("MOTD", "General", "ignore", true, null, 128)]
        public string MOTD = "ignore";

        // Permission settings
        [ConfigBool("Buildable", "Permissions", true)]
        public bool Buildable = true;
        [ConfigBool("Deletable", "Permissions", true)]
        public bool Deletable = true;

        [ConfigPerm("PerBuild", "Permissions", LevelPermission.Guest)]
        public LevelPermission BuildMin = LevelPermission.Guest;
        [ConfigPerm("PerBuildMax", "Permissions", LevelPermission.Nobody)]
        public LevelPermission BuildMax = LevelPermission.Nobody;
        
        // Other blacklists/whitelists
        [ConfigStringList("BuildWhitelist", "Permissions")]
        public List<string> BuildWhitelist = new List<string>();
        [ConfigStringList("BuildBlacklist", "Permissions")]
        public List<string> BuildBlacklist = new List<string>();
        

        // Environment settings
        const int envRange = 0xFFFFFF;
        [ConfigInt("Weather", "Env", 0, 0, 2)]
        public int Weather = -1;
        /// <summary> Elevation of the "ocean" that surrounds maps. Default is map height / 2. </summary>
        [ConfigInt("EdgeLevel", "Env", -1, -envRange, envRange)]
        public int EdgeLevel = -1;
        /// <summary> Offset of the "bedrock" that surrounds map sides from edge level. Default is -2. </summary>
        [ConfigInt("SidesOffset", "Env", -2, -envRange, envRange)]
        public int SidesOffset = -1;
        /// <summary> Elevation of the clouds. Default is map height + 2. </summary>
        [ConfigInt("CloudsHeight", "Env", -1, -envRange, envRange)]
        public int CloudsHeight = -1;
        
        /// <summary> Max fog distance the client can see. Default is 0, means use client-side defined max fog distance. </summary>
        [ConfigInt("MaxFog", "Env", 0, -envRange, envRange)]
        public int MaxFogDistance = -1;
        /// <summary> Clouds speed, in units of 256ths. Default is 256 (1 speed). </summary>
        [ConfigInt("clouds-speed", "Env", 256, -envRange, envRange)]
        public int CloudsSpeed = -1;
        /// <summary> Weather speed, in units of 256ths. Default is 256 (1 speed). </summary>
        [ConfigInt("weather-speed", "Env", 256, -envRange, envRange)]
        public int WeatherSpeed = -1;
        /// <summary> Weather fade, in units of 256ths. Default is 256 (1 speed). </summary>
        [ConfigInt("weather-fade", "Env", 128, -envRange, envRange)]
        public int WeatherFade = -1;
        /// <summary> Skybox horizontal speed, in units of 1024ths. Default is 0 (0 speed). </summary>
        [ConfigInt("skybox-hor-speed", "Env", 0, -envRange, envRange)]
        public int SkyboxHorSpeed = -1;
        /// <summary> Skybox vertical speed, in units of 1024ths. Default is 0 (0 speed). </summary>
        [ConfigInt("skybox-ver-speed", "Env", 0, -envRange, envRange)]
        public int SkyboxVerSpeed = -1;
        
        /// <summary> The block which will be displayed on the horizon. </summary>
        [ConfigBlock("HorizonBlock", "Env", Block.Water)]
        public BlockID HorizonBlock = Block.Invalid;
        /// <summary> The block which will be displayed on the edge of the map. </summary>
        [ConfigBlock("EdgeBlock", "Env", Block.Bedrock)]
        public BlockID EdgeBlock = Block.Invalid;
        /// <summary> Whether exponential fog mode is used client-side. </summary>
        [ConfigBool("ExpFog", "Env", false)]
        public bool ExpFog;
        [ConfigString("Texture", "Env", "", true, null, NetUtils.StringSize)]
        public string Terrain = "";
        [ConfigString("TexturePack", "Env", "", true, null, NetUtils.StringSize)]
        public string TexturePack = "";
        
        /// <summary> Color of the clouds (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("CloudColor", "Env", "", true)]
        public string CloudColor = "";
        /// <summary> Color of the fog (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("FogColor", "Env", "", true)]
        public string FogColor = "";
        /// <summary> Color of the sky (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("SkyColor", "Env", "", true)]
        public string SkyColor = "";
        /// <summary> Color of the blocks in shadows (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("ShadowColor", "Env", "", true)]
        public string ShadowColor = "";
        /// <summary> Color of the blocks in the light (RGB packed into an int). Set to -1 to use client defaults. </summary>
        [ConfigString("LightColor", "Env", "", true)]
        public string LightColor = "";
        
        public void Reset(int height) {
            Weather = 0;
            EdgeLevel = height / 2;
            SidesOffset = -2;
            CloudsHeight = height + 2;
            
            MaxFogDistance = 0;
            CloudsSpeed = 256;
            WeatherSpeed = 256;
            WeatherFade = 128;
            SkyboxHorSpeed = 0;
            SkyboxVerSpeed = 0;
            
            HorizonBlock = Block.Water;
            EdgeBlock = Block.Bedrock;
            ExpFog = false;
            
            Terrain = "";
            TexturePack = "";
            CloudColor = "";
            FogColor = "";
            SkyColor = "";
            ShadowColor = "";
            LightColor = "";
        }
        
        public string GetColor(int i) {
            if (i == 0) return SkyColor;
            if (i == 1) return CloudColor;
            if (i == 2) return FogColor;
            if (i == 3) return ShadowColor;
            if (i == 4) return LightColor;
            return null;
        }
    }
    
    public sealed class LevelConfig : AreaConfig {
        [ConfigBool("LoadOnGoto", "General", true)]
        public bool LoadOnGoto = true;
        [ConfigString("Theme", "General", "Normal", true)]
        public string Theme = "Normal";
        [ConfigString("Seed", "General", "", true)]
        public string Seed = "";
        [ConfigBool("Unload", "General", true)]
        public bool AutoUnload = true;
        /// <summary> true if this map may see server-wide chat, false if this map has level-only/isolated chat </summary>
        [ConfigBool("WorldChat", "General", true)]
        public bool ServerWideChat = true;

        [ConfigBool("UseBlockDB", "Other", true)]
        public bool UseBlockDB = true;
        [ConfigInt("LoadDelay", "Other", 0, 0, 2000)]
        public int LoadDelay = 0;
        
        public byte jailrotx, jailroty;
        [ConfigInt("JailX", "Jail", 0, 0, 65535)]
        public int JailX;
        [ConfigInt("JailY", "Jail", 0, 0, 65535)]
        public int JailY;
        [ConfigInt("JailZ", "Jail", 0, 0, 65535)]
        public int JailZ;
        
        // Permission settings
        [ConfigString("RealmOwner", "Permissions", "", true)]
        public string RealmOwner = "";
        [ConfigPerm("PerVisit", "Permissions", LevelPermission.Guest)]
        public LevelPermission VisitMin = LevelPermission.Guest;
        [ConfigPerm("PerVisitMax", "Permissions", LevelPermission.Nobody)]
        public LevelPermission VisitMax = LevelPermission.Nobody;
        
        // Other blacklists/whitelists
        [ConfigStringList("VisitWhitelist", "Permissions")]
        public List<string> VisitWhitelist = new List<string>();
        [ConfigStringList("VisitBlacklist", "Permissions")]
        public List<string> VisitBlacklist = new List<string>();
        
        // Physics settings
        [ConfigInt("Physics", "Physics", 0, 0, 5)]
        public int Physics;
        [ConfigInt("Physics overload", "Physics", 1500)]
        public int PhysicsOverload = 1500;
        [ConfigInt("Physics speed", "Physics", 250)]
        public int PhysicsSpeed = 250;
        [ConfigBool("RandomFlow", "Physics", true)]
        public bool RandomFlow = true;
        [ConfigBool("LeafDecay", "Physics", false)]
        public bool LeafDecay;
        [ConfigBool("Finite mode", "Physics", false)]
        public bool FiniteLiquids;
        [ConfigBool("GrowTrees", "Physics", false)]
        public bool GrowTrees;
        [ConfigBool("Animal AI", "Physics", true)]
        public bool AnimalHuntAI = true;
        [ConfigBool("GrassGrowth", "Physics", true)]
        public bool GrassGrow = true;
        [ConfigString("TreeType", "Physics", "fern", false)]
        public string TreeType = "fern";
        
        // Survival settings
        [ConfigInt("Drown", "Survival", 70)]
        public int DrownTime = 70;
        [ConfigBool("Edge water", "Survival", true)]
        public bool EdgeWater;
        [ConfigInt("Fall", "Survival", 9)]
        public int FallHeight = 9;
        [ConfigBool("Guns", "Survival", false)]
        public bool Guns = false;
        [ConfigBool("Survival death", "Survival", false)]
        public bool SurvivalDeath;
        [ConfigBool("Killer blocks", "Survival", true)]
        public bool KillerBlocks = true;
        
        // Games settings
        [ConfigInt("Likes", "Game", 0)]
        public int Likes;
        [ConfigInt("Dislikes", "Game", 0)]
        public int Dislikes;
        [ConfigString("Authors", "Game", "", true)]
        public string Authors = "";
        [ConfigBool("Pillaring", "Game", false)]
        public bool Pillaring = !ZSConfig.NoPillaring;
        
        [ConfigEnum("BuildType", "Game", BuildType.Normal, typeof(BuildType))]
        public BuildType BuildType = BuildType.Normal;
        
        [ConfigInt("MinRoundTime", "Game", 4)]
        public int MinRoundTime = 4;
        [ConfigInt("MaxRoundTime", "Game", 7)]
        public int MaxRoundTime = 7;
        [ConfigBool("DrawingAllowed", "Game", true)]
        public bool DrawingAllowed = true;
        [ConfigInt("RoundsPlayed", "Game", 0)]
        public int RoundsPlayed = 0;
        [ConfigInt("RoundsHumanWon", "Game", 0)]
        public int RoundsHumanWon = 0;

        // CTF settings.

        [ConfigInt("CTFDivider", "CTF", 0)]
        public int CTFDivider = 0;
        [ConfigInt("CTFBuildCeiling", "CTF", 0)]
        public int CTFBuildCeiling = 0;
        [ConfigInt("CTFBuildFloor", "CTF",-8)]
        public int CTFBuildFloor = -8;

        [ConfigInt("CTFRedSpawnX", "CTF", 0)]
        public int CTFRedSpawnX = 0;
        [ConfigInt("CTFRedSpawnY", "CTF", 0)]
        public int CTFRedSpawnY = 0;
        [ConfigInt("CTFRedSpawnZ", "CTF", 0)]
        public int CTFRedSpawnZ = 0;
        [ConfigInt("CTFBlueSpawnX", "CTF", 0)]
        public int CTFBlueSpawnX = 0;
        [ConfigInt("CTFBlueSpawnY", "CTF", 0)]
        public int CTFBlueSpawnY = 0;
        [ConfigInt("CTFBlueSpawnZ", "CTF", 0)]
        public int CTFBlueSpawnZ = 0;

        [ConfigInt("CTFRedFlagX", "CTF", 0)]
        public int CTFRedFlagX = 0;
        [ConfigInt("CTFRedFlagY", "CTF", 0)]
        public int CTFRedFlagY = 0;
        [ConfigInt("CTFRedFlagZ", "CTF", 0)]
        public int CTFRedFlagZ = 0;
        [ConfigInt("CTFBlueFlagX", "CTF", 0)]
        public int CTFBlueFlagX = 0;
        [ConfigInt("CTFBlueFlagY", "CTF", 0)]
        public int CTFBlueFlagY = 0;
        [ConfigInt("CTFBlueFlagZ", "CTF", 0)]
        public int CTFBlueFlagZ = 0;

        [ConfigString("CTFSolidBlocks", "CTF", "")]
        public string CTFSolidBlocks = "";

        [ConfigString("CTFMapAuthor", "CTF", "Unknown")]
        public string CTFMapAuthor = "Unknown";

        public string Color {
            get {
                LevelPermission maxPerm = VisitMin;
                if (maxPerm < BuildMin) maxPerm = BuildMin;
                return Group.GetColor(maxPerm);
            }
        }
        
        
        public static bool Load(string path, LevelConfig config) {
            return PropertiesFile.Read(path, ref config, LineProcessor);
        }
        
        static void LineProcessor(string key, string value, ref LevelConfig config) {
            if (!ConfigElement.Parse(Server.levelConfig, key, value, config)) {
                Logger.Log(LogType.Warning, "\"{0}\" was not a recognised level property key.", key);
            }
        }
        
        public static void Save(string path, LevelConfig config, string lvlname) {
            try {
                using (StreamWriter w = new StreamWriter(path)) {
                    w.WriteLine("#Level properties for " + lvlname);
                    w.WriteLine("#Drown-time in seconds is [drown time] * 200 / 3 / 1000");
                    ConfigElement.Serialise(Server.levelConfig, " settings", w, config);
                }
            } catch (Exception ex) {
                Logger.Log(LogType.Warning, "Failed to save level properties!");
                Logger.LogError(ex);
            }
        }
    }
}