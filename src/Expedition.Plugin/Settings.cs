using BepInEx.Configuration;
using UnityEngine;

namespace Expedition.Plugin
{
    // On/off switches for each feature. BepInEx saves them to
    // BepInEx\config\com.lynztighers.expedition.cfg, which players can edit with Notepad.
    static class Settings
    {
        // Bumped when a default changes in a way existing config files should pick up.
        const int CurrentConfigVersion = 2;

        public static ConfigEntry<bool> Nameplates;
        public static ConfigEntry<bool> Pings;
        public static ConfigEntry<KeyCode> PingKey;
        public static ConfigEntry<float> PingSeconds;
        public static ConfigEntry<bool> MapMarkers;
        public static ConfigEntry<bool> ShowSelfOnMap;
        public static ConfigEntry<bool> ShareQuestRewards;
        public static ConfigEntry<bool> ShareSilver;
        public static ConfigEntry<bool> ShareLoot;
        static ConfigEntry<int> configVersion;

        public static void Bind(ConfigFile config)
        {
            Nameplates = config.Bind("Party", "Nameplates", true,
                "Show other players' names on their health bar, at any distance.");
            Pings = config.Bind("Party", "Pings", true,
                "Press the ping key to mark the spot in front of you for every player.");
            PingKey = config.Bind("Party", "PingKey", KeyCode.G,
                "Key that places a ping. Examples: G, T, Mouse2 (middle mouse), Mouse3.");
            PingSeconds = config.Bind("Party", "PingSeconds", 20f,
                "How many seconds a ping stays on screen.");
            MapMarkers = config.Bind("Party", "MapMarkers", true,
                "Show every player, in their party colour, on the big map (and pings too).");
            ShowSelfOnMap = config.Bind("Party", "ShowSelfOnMap", true,
                "Also show your own position on the big map.");
            ShareQuestRewards = config.Bind("Sharing", "ShareQuestRewards", true,
                "Quest rewards (items, skills, silver, XP) go to every player. Applied by the host.");
            ShareSilver = config.Bind("Sharing", "ShareSilver", true,
                "Silver looted from chests, enemies and the ground is split evenly between players.");
            ShareLoot = config.Bind("Sharing", "ShareLoot", true,
                "When someone takes an item from a chest or enemy, every other player gets their own copy.");
            configVersion = config.Bind("Internal", "ConfigVersion", 1,
                "Used by Expedition to update old settings. Don't change this.");

            Migrate();
        }

        // BepInEx keeps a value once it's written to the file, so a new default only reaches
        // new players. Config version 1 (Expedition 0.2.0) wrote PingKey = Mouse2; move anyone
        // still on that old default to G.
        static void Migrate()
        {
            if (configVersion.Value >= CurrentConfigVersion)
            {
                return;
            }

            if (PingKey.Value == KeyCode.Mouse2)
            {
                PingKey.Value = KeyCode.G;
            }
            configVersion.Value = CurrentConfigVersion;
        }
    }
}
