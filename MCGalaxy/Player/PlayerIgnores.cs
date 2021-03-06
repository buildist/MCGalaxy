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
using System.IO;

namespace MCGalaxy {
    
    public class PlayerIgnores {
        public List<string> Names = new List<string>(), IRCNicks = new List<string>();
        public bool All, IRC, Titles, Nicks, EightBall, DrawOutput;
        
        public void Load(Player p) {
            string path = "ranks/ignore/" + p.name + ".txt";
            if (!File.Exists(path)) return;
            
            try {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines) {
                    if (line == "&global") continue; // deprecated /ignore global
                    if (line == "&all") { All = true; continue; }
                    if (line == "&irc") { IRC = true; continue; }
                    if (line == "&8ball") { EightBall = true; continue; }
                    if (line == "&drawoutput") { DrawOutput = true; continue; }
                    if (line == "&titles") { Titles = true; continue; }
                    if (line == "&nicks") { Nicks = true; continue; }
                    
                    if (line.StartsWith("&irc_")) {
                        IRCNicks.Add(line.Substring("&irc_".Length));
                    } else {
                        Names.Add(line);
                    }
                }
            } catch (IOException ex) {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "Failed to load ignore list for: " + p.name);
            }
            
            if (All || IRC || EightBall || DrawOutput || Titles || Nicks || Names.Count > 0 || IRCNicks.Count > 0) {
                Player.Message(p, "&cType &a/ignore list &cto see who you are still ignoring");
            }
        }
        
        public void Save(Player p) {
            string path = "ranks/ignore/" + p.name + ".txt";
            if (!Directory.Exists("ranks/ignore"))
                Directory.CreateDirectory("ranks/ignore");
            
            try {
                using (StreamWriter w = new StreamWriter(path)) {
                    if (All) w.WriteLine("&all");
                    if (IRC) w.WriteLine("&irc");
                    if (EightBall) w.WriteLine("&8ball");
                    
                    if (DrawOutput) w.WriteLine("&drawoutput");
                    if (Titles) w.WriteLine("&titles");
                    if (Nicks) w.WriteLine("&nicks");
                    
                    foreach (string nick in IRCNicks) { w.WriteLine("&irc_" + nick); }
                    foreach (string name in Names) { w.WriteLine(name); }
                }
            } catch (IOException ex) {
                Logger.LogError(ex);
                Logger.Log(LogType.Warning, "Failed to save ignored list for player: " + p.name);
            }
        }
        
        public void Output(Player p) {
            if (Names.Count > 0) {
                Player.Message(p, "&cCurrently ignoring the following players:");
                Player.Message(p, Names.Join(n => PlayerInfo.GetColoredName(p, n)));
            }
            if (IRCNicks.Count > 0) {
                Player.Message(p, "&cCurrently ignoring the following IRC nicks:");
                Player.Message(p, IRCNicks.Join());
            }
            
            if (All) Player.Message(p, "&cIgnoring all chat");
            if (IRC) Player.Message(p, "&cIgnoring IRC chat");
            if (EightBall) Player.Message(p, "&cIgnoring %T/8ball");
            
            if (DrawOutput) Player.Message(p, "&cIgnoring draw command output.");
            if (Titles) Player.Message(p, "&cPlayer titles do not show before names in chat.");
            if (Nicks) Player.Message(p, "&cCustom player nicks do not show in chat.");
        }
    }
}