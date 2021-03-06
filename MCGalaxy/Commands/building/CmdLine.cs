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
using System;
using MCGalaxy.Drawing.Brushes;
using MCGalaxy.Drawing.Ops;
using MCGalaxy.Maths;

namespace MCGalaxy.Commands.Building {
    public sealed class CmdLine : DrawCmd {
        public override string name { get { return "Line"; } }
        public override string shortcut { get { return "l"; } }
        protected override string PlaceMessage { get { return "Place or break two blocks to determine the endpoints."; } }
        
        protected override DrawMode GetMode(string[] parts) {
            string mode = parts[parts.Length - 1];
            if (mode.Length == 0) return DrawMode.normal;
            DrawMode dMode = ParseMode(mode);
            if (dMode != DrawMode.normal) return dMode;
            
            // May be in the format <brush args> <mode> <max_length>
            ushort len;
            if (parts.Length == 1 || !ushort.TryParse(mode, out len))
                return DrawMode.normal;
            return ParseMode(parts[parts.Length - 2]);
        }
        
        static DrawMode ParseMode(string msg) {
            if (msg == "normal")   return DrawMode.solid;
            if (msg == "walls")    return DrawMode.walls;
            if (msg == "straight") return DrawMode.straight;
            return DrawMode.normal;
        }
        
        protected override DrawOp GetDrawOp(DrawArgs dArgs) {
            LineDrawOp line = new LineDrawOp();
            line.WallsMode = dArgs.Mode == DrawMode.walls;
            string msg = dArgs.Message;
            if (msg.IndexOf(' ') == -1 || dArgs.Mode == DrawMode.normal) return line;
            
            string arg = msg.Substring(msg.LastIndexOf(' ') + 1);
            ushort len;
            if (ushort.TryParse(arg, out len))
                line.MaxLength = len;
            return line;
        }
        
        protected override void GetMarks(DrawArgs dArgs, ref Vec3S32[] m) {
            if (dArgs.Mode != DrawMode.straight) return;
            int dx = Math.Abs(m[0].X - m[1].X), dy = Math.Abs(m[0].Y - m[1].Y), dz = Math.Abs(m[0].Z - m[1].Z);

            if (dx > dy && dx > dz) {
                m[1].Y = m[0].Y; m[1].Z = m[0].Z;
            } else if (dy > dx && dy > dz) {
                m[1].X = m[0].X; m[1].Z = m[0].Z;
            } else if (dz > dy && dz > dx) {
                m[1].X = m[0].X; m[1].Y = m[0].Y;
            }
        }
        
        protected override void GetBrush(DrawArgs dArgs) {
            LineDrawOp line = (LineDrawOp)dArgs.Op;
            int endCount = dArgs.DefaultBrushEndCount;
            if (line.MaxLength != int.MaxValue) endCount++;
            dArgs.BrushArgs = dArgs.Message.Splice(0, endCount);
        }
        
        public override void Help(Player p) {
            Player.Message(p, "%T/Line <brush args> <mode> <length>");
            Player.Message(p, "%HCreates a line between two points.");
            Player.Message(p, "   %HModes: &fnormal/walls/straight");
            Player.Message(p, "   %HLength specifies the max number of blocks in the line.");
            Player.Message(p, BrushHelpLine);
        }
    }
}
