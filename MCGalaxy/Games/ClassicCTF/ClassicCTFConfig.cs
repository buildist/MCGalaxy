using MCGalaxy.Config;

namespace MCGalaxy.Games {
    public static class ClassicCTFConfig {
        [ConfigString("stats-post-url", "CTF", "")]
        public static string StatsPostUrl;

        [ConfigInt("secret", "CTF", 0)]
        public static int Secret;

        [ConfigString("help-text", "CTF", "Try to capture the other team's flag and bring it back " +
        "to your own side. Click the other team's flag to take it; capture it by clicking your own " +
        "flag. You can stop the enemy by tagging them when they're on your side of the map, blowing" +
        " them up with TNT (place a TNT block, then place a purple block or say /t to explode it), " +
        "cooking them with the flamethrower (/f) or placing landmines (dark gray blocks). You gain " +
        "points for doing well; say /store to find out how many points you have and what you can " +
        "buy.")]
        public static string HelpText;

        [ConfigString("tdm-help-text", "CTF", "This is Team Deathmatch mode. You can kill players " +
        "on the other team by blowing them up with TNT (place a TNT block, then place a purple " +
        "block or say /t to explode it), cooking them with the flamethrower (/f) or placing " +
        "landmines (dark gray blocks). You gain points for doing well; say /store to find out how " +
        "many points you have and what you can buy. The team with the most kills after 10 minutes " +
        "wins. (Use the ClassiCube client to see a timer!)")]
        public static string TDMHelpText;

        [ConfigString("ctf-welcome", "CTF", "Play Capture the Flag here!")]
        public static string CTFWelcomeMessage;

        static string SendChatUrl {
            get {
                return "http://buildism.net/mc/server/sendChat.php?k=" + Secret;
            }
        }

        static string ServerStatusUrl {
            get {
                return "http://buildism.net/mc/server/serverStatus.php?k=" + Secret;
            }
        }

        static string MapCommentUrl {
            get {
                return "http://buildism.net/mc/server/mapComment.php?k=" + Secret;
            }
        }
    }
}
