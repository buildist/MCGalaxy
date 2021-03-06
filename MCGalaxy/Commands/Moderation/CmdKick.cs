/*
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
using MCGalaxy.Events;

namespace MCGalaxy.Commands.Moderation {
    public sealed class CmdKick : Command {
        public override string name { get { return "Kick"; } }
        public override string shortcut { get { return "k"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        
        public override void Use(Player p, string message) {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);
            
            Player who = PlayerInfo.FindMatches(p, args[0]);
            if (who == null) return;
            
            string kickMsg = null, reason = null;
            if (p == null) kickMsg = "by (console)";
            else kickMsg = "by " + p.truename;
            
            if (args.Length > 1) {
                reason = ModActionCmd.ExpandReason(p, args[1]);
                if (message == null) return;
                kickMsg += "&f: " + reason; 
            }

            if (p != null && p == who) { Player.Message(p, "You cannot kick yourself."); return; }
            if (p != null && who.Rank >= p.Rank) {
                Chat.MessageGlobal(p, p.ColoredName + " %Stried to kick "
                                   + who.ColoredName + " %Sbut failed.", false);
                return;
            }
            
            ModAction action = new ModAction(who.name, p, ModActionType.Kicked, reason);
            OnModActionEvent.Call(action);
            who.Kick(kickMsg, "Kicked " + kickMsg);
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Kick [player] <reason>");
            Player.Message(p, "%HKicks a player.");
            Player.Message(p, "%HFor <reason>, @number can be used as a shortcut for that rule.");
        }
    }
}
