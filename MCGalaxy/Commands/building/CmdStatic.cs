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
namespace MCGalaxy.Commands.Building {   
    public sealed class CmdStatic : Command {      
        public override string name { get { return "Static"; } }
        public override string shortcut { get { return "t"; } }
        public override string type { get { return CommandTypes.Building; } }
        public override bool museumUsable { get { return false; } }
        public override LevelPermission defaultRank { get { return LevelPermission.AdvBuilder; } }
        public override bool SuperUseable { get { return false; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("zz", "cuboid") }; }
        }

        public override void Use(Player p, string message) {
            p.staticCommands = !p.staticCommands;
            p.ClearBlockchange();
            p.ModeBlock = Block.Air;

            Player.Message(p, "Static mode: &a" + p.staticCommands);
            if (message.Length == 0 || !p.staticCommands) return;

            string[] parts = message.SplitSpaces(2);
            string cmdName = parts[0], cmdArgs = parts.Length > 1 ? parts[1] : "";
            Command.Search(ref cmdName, ref cmdArgs);
            
            Command cmd = Command.all.FindByName(cmdName);
            if (cmd == null) {
                Player.Message(p, "Unknown command \"" + cmdName + "\"."); return;
            }
            
            if (!p.group.CanExecute(cmd)) {
                Player.Message(p, "Cannot use the \"{0}\" command.", cmdName); return;
            }
            cmd.Use(p, cmdArgs);
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Static [command]");
            Player.Message(p, "%HMakes every command a toggle.");
            Player.Message(p, "%HIf [command] is given, then that command is used");
        }
    }
}
