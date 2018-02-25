namespace MCGalaxy.Games {
    public class Mine {
        public readonly ushort x;
        public readonly ushort y;
        public readonly ushort z;
        public CTFTeam team;
        public Player owner;
        public bool active = false;

        public Mine(ushort x, ushort y, ushort z, CTFTeam team, Player owner) {
            this.x = x;
            this.y = y;
            this.z = z;
            this.team = team;
            this.owner = owner;
        }
    }
}
