/*
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
using MCGalaxy.Games;

namespace MCGalaxy.Commands.Fun {    
    public sealed class CmdInfected : Command {
        public override string name { get { return "Infected"; } }
        public override string shortcut { get { return "dead"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override CommandEnable Enabled { get { return CommandEnable.Zombie; } }

        public override void Use(Player p, string message) {
            Player[] infected = Server.zombie.Infected.Items;
            if (infected.Length == 0) { Player.Message(p, "No one is infected"); return; }
            
            Player.Message(p, "Players who are &cinfected %Sare:");
            Player.Message(p, infected.Join(pl => "&c" + pl.DisplayName + "%S"));
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Infected");
            Player.Message(p, "%HShows who is infected/a zombie");
        }
    }
}
