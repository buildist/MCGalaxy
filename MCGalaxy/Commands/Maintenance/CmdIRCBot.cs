/*
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
namespace MCGalaxy.Commands.Maintenance {
    public sealed class CmdIRCBot : Command {
        public override string name { get { return "IRCBot"; } }
        public override string type { get { return CommandTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("ResetBot", "reset"), new CommandAlias("ResetIRC", "reset") }; }
        }

        public override void Use(Player p, string message) {
            if (message.CaselessEq("reset") || message.CaselessEq("reconnect")) {
                if (!ServerConfig.UseIRC) { Player.Message(p, "The server does not have IRC enabled."); }
                Server.IRC.Reset();
            } else if (message.CaselessEq("connect")) {
                if (!ServerConfig.UseIRC) { Player.Message(p, "The server does not have IRC enabled."); }
                Server.IRC.Connect();
            } else if (message.CaselessEq("disconnect")) {
                Server.IRC.Disconnect("Disconnecting IRC bot");
            } else {
                Help(p);
            }
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/IRCBot connect");
            Player.Message(p, "%HCauses the IRC bot to connect to IRC.");
            Player.Message(p, "%T/IRCBot disconnect");
            Player.Message(p, "%HCauses the IRC bot to disconnect from IRC.");
            Player.Message(p, "%T/IRCBot reset");
            Player.Message(p, "%HCauses the IRC bot to disconnect then reconnect.");
        }
    }
}
