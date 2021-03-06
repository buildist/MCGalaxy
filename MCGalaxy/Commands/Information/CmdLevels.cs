/*
    Copyright 2012 MCForge
    
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

namespace MCGalaxy.Commands.Info {   
    public sealed class CmdLevels : Command {       
        public override string name { get { return "Levels"; } }
        public override string shortcut { get { return "Maps"; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool UseableWhenFrozen { get { return true; } }
        
        public override void Use(Player p, string message) {
            Level[] loaded = LevelInfo.Loaded.Items;
            Player.Message(p, "Loaded maps [physics level] (&c[no] %Sif not visitable): ");
            MultiPageOutput.Output(p, loaded, (lvl) => FormatMap(p, lvl),
                                   "Levels", "maps", message, false);
            Player.Message(p, "Use %T/Worlds %Sfor all levels.");
        }
        
        static string FormatMap(Player p, Level lvl) {            
            bool canVisit = Player.IsSuper(p);
            if (!canVisit) {
                AccessResult access = lvl.VisitAccess.Check(p);
                canVisit = access == AccessResult.Allowed || access == AccessResult.Whitelisted;
            }
            
            string physics = " [" +  lvl.physics + "]";
            string visit = canVisit ? "" : " &c[no]";
            return lvl.ColoredName + physics + visit;
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Levels");
            Player.Message(p, "%HLists all loaded levels and their physics levels.");
        }
    }
}
