/*
    Copyright 2011 MCForge
    
    Made originally by 501st_commander, in something called SharpDevelop.
    Made into a safe and reasonabal command by EricKilla, in Visual Studio 2010.
    
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
using MCGalaxy.Tasks;

namespace MCGalaxy.Commands.Misc {
    
    public sealed class CmdHackRank : Command {
        public override string name { get { return "HackRank"; } }
        public override string type { get { return CommandTypes.Other; } }
        public override bool SuperUseable { get { return false; } }

        public override void Use(Player p, string message) {
            if (message.Length == 0) { Help(p); return; }
            
            if (p.hackrank) {
                Player.Message(p, Colors.red + "You have already hacked a rank!"); return;
            }
            
            Group grp = Matcher.FindRanks(p, message);
            if (grp == null) return;
            DoFakeRank(p, grp);
        }

        void DoFakeRank(Player p, Group newRank) {
            p.hackrank = true;
            CmdFakeRank.DoFakerank(p, p, newRank);
            DoKick(p, newRank);
        }

        void DoKick(Player p, Group newRank) {
            if (!ServerConfig.HackrankKicks) return;
            HackRankArgs args = new HackRankArgs();
            args.name = p.name; args.newRank = newRank;
            
            TimeSpan delay = TimeSpan.FromSeconds(ServerConfig.HackrankKickDelay);
            Server.MainScheduler.QueueOnce(HackRankCallback, args, delay);
        }
        
        void HackRankCallback(SchedulerTask task) {
            HackRankArgs args = (HackRankArgs)task.State;
            Player who = PlayerInfo.FindExact(args.name);
            if (who == null) return;

            string msg = "for hacking the rank " + args.newRank.ColoredName;
            who.Leave("kicked (" + msg + "%S)", "Kicked " + msg);
        }
        
        class HackRankArgs { public string name; public Group newRank; }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/HackRank [rank] %H- Hacks a rank");
            Player.Message(p, "%HTo see available ranks, type %T/ViewRanks");
        }
    }
}