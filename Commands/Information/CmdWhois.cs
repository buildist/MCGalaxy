/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
    
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
namespace MCGalaxy.Commands
{
    public sealed class CmdWhois : Command
    {
        public override string name { get { return "whois"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.Information; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }

        public override void Use(Player p, string message) {
            Player who = message == "" ? p : PlayerInfo.Find(message);
            if (message == "") message = p.name;
            if (who == null || who.hidden) {
                Player.SendMessage(p, "\"" + message + "\" is offline! Using /whowas instead.");
                Command.all.Find("whowas").Use(p, message); return;
            }

            Player.SendMessage(p, who.color + who.name + Server.DefaultColor + " (" + who.DisplayName + ") " + Server.DefaultColor + " is on &b" + who.level.name);
            Player.SendMessage(p, who.color + who.prefix + who.name + Server.DefaultColor + " has :");
            Player.SendMessage(p, "> > the rank of " + who.group.color + who.group.name);
            
            Group nobody = Group.findPerm(LevelPermission.Nobody);
            if (nobody == null || (!nobody.commands.Contains("pay") && !nobody.commands.Contains("give") && !nobody.commands.Contains("take")))
                Player.SendMessage(p, "> > &a" + who.money + Server.DefaultColor + " " + Server.moneys);

            Player.SendMessage(p, "> > &cdied &a" + who.overallDeath + Server.DefaultColor + " times");
            Player.SendMessage(p, "> > &bmodified &a" + who.overallBlocks + " &eblocks &eand &a" + who.loginBlocks + " &ewere changed &9since logging in&e.");
            string storedTime = Convert.ToDateTime(DateTime.Now.Subtract(who.timeLogged).ToString()).ToString("HH:mm:ss");
            TimeSpan time = who.time;
            Player.SendMessage(p, "> > time spent on server: " + time.Days + " Days, " + time.Hours + " Hours, " + time.Minutes + " Minutes, " + time.Seconds + " Seconds.");
            Player.SendMessage(p, "> > been logged in for &a" + storedTime);
            Player.SendMessage(p, "> > first logged into the server on &a" + who.firstLogin.ToString("yyyy-MM-dd") + " at " + who.firstLogin.ToString("HH:mm:ss"));
            Player.SendMessage(p, "> > logged in &a" + who.totalLogins + Server.DefaultColor + " times, &c" + who.totalKicked + Server.DefaultColor + " of which ended in a kick.");
            Player.SendMessage(p, "> > " + Awards.awardAmount(who.name) + " awards");
            if (Ban.IsBanned(who.name)) {
                string[] data = Ban.GetBanData(who.name);
                Player.SendMessage(p, "> > is banned for " + data[1] + " by " + data[0]);
            }

            if (who.isDev) Player.SendMessage(p, "> > Player is a &9Developer");
            else if (who.isMod) Player.SendMessage(p, "> > Player is a &9MCGalaxy Moderator");
            else if (who.isGCMod) Player.SendMessage(p, "> > Player is a &9Global Chat Moderator");

            if (p == null || (int)p.group.Permission <= CommandOtherPerms.GetPerm(this)) {
                string givenIP;
                if (Server.bannedIP.Contains(who.ip)) givenIP = "&8" + who.ip + ", which is banned";
                else givenIP = who.ip;
                Player.SendMessage(p, "> > the IP of " + givenIP);
                if (Server.useWhitelist&& Server.whiteList.Contains(who.name))
                    Player.SendMessage(p, "> > Player is &fWhitelisted");
            }        
        }
        
        public override void Help(Player p) {
            Player.SendMessage(p, "/whois [player] - Displays information about someone.");
        }
    }
}
