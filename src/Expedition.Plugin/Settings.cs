using BepInEx.Configuration;
using UnityEngine;

namespace Expedition.Plugin
{
    // On/off switches for each feature. BepInEx saves them to
    // BepInEx\config\com.lynztighers.expedition.cfg, which players can edit with Notepad.
    static class Settings
    {
        public static ConfigEntry<bool> Nameplates;
        public static ConfigEntry<bool> Pings;
        public static ConfigEntry<KeyCode> PingKey;
        public static ConfigEntry<float> PingSeconds;
        public static ConfigEntry<bool> ShareQuestRewards;
        public static ConfigEntry<bool> ShareSilver;
        public static ConfigEntry<bool> ShareLoot;

        public static void Bind(ConfigFile config)
        {
            Nameplates = config.Bind("Party", "Nameplates", true,
                "Show other players' names on their health bar, at any distance.");
            Pings = config.Bind("Party", "Pings", true,
                "Press the ping key to mark the spot in front of you for every player.");
            PingKey = config.Bind("Party", "PingKey", KeyCode.Mouse2,
                "Key that places a ping. Mouse2 is the middle mouse button. Examples: G, T, Mouse3.");
            PingSeconds = config.Bind("Party", "PingSeconds", 20f,
                "How many seconds a ping stays on screen.");
            ShareQuestRewards = config.Bind("Sharing", "ShareQuestRewards", true,
                "Quest rewards (items, skills, silver, XP) go to every player. Applied by the host.");
            ShareSilver = config.Bind("Sharing", "ShareSilver", true,
                "Silver looted from chests, enemies and the ground is split evenly between players.");
            ShareLoot = config.Bind("Sharing", "ShareLoot", true,
                "When someone takes an item from a chest or enemy, every other player gets their own copy.");
        }
    }
}
