using BepInEx.Configuration;

namespace Expedition.Plugin
{
    // On/off switches for each feature. BepInEx saves them to
    // BepInEx\config\com.lynztighers.expedition.cfg, which players can edit with Notepad.
    static class Settings
    {
        public static ConfigEntry<bool> Nameplates;
        public static ConfigEntry<bool> ShareQuestRewards;
        public static ConfigEntry<bool> ShareSilver;
        public static ConfigEntry<bool> ShareLoot;

        public static void Bind(ConfigFile config)
        {
            Nameplates = config.Bind("Party", "Nameplates", true,
                "Show other players' names on their health bar, at any distance.");
            ShareQuestRewards = config.Bind("Sharing", "ShareQuestRewards", true,
                "Quest rewards (items, skills, silver, XP) go to every player. Applied by the host.");
            ShareSilver = config.Bind("Sharing", "ShareSilver", true,
                "Silver looted from chests, enemies and the ground is split evenly between players.");
            ShareLoot = config.Bind("Sharing", "ShareLoot", true,
                "When someone takes an item from a chest or enemy, every other player gets their own copy.");
        }
    }
}
