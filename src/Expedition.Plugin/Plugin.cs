using BepInEx;
using HarmonyLib;


namespace Expedition.Plugin
{

    [BepInPlugin("com.lynztighers.expedition", "Expedition", "0.3.0")]
    public class Plugin : BaseUnityPlugin

    {
        private void Awake()
        {
            Logger.LogInfo("THis mod has loaded!");

            // Reads (or creates) BepInEx\config\com.lynztighers.expedition.cfg.
            Settings.Bind(Config);

            // Applies every [HarmonyPatch] class in this mod (see the Patches folder).
            new Harmony("com.lynztighers.expedition").PatchAll();
            Logger.LogInfo("Room limit raised to " + Patches.RoomLimit.MaxPlayers + " players.");

            Pings.Create();
            PartyMap.Create();
            Logger.LogInfo("Pings: " + Settings.Pings.Value + " (key: " + Settings.PingKey.Value + ")"
                + ", map markers: " + Settings.MapMarkers.Value);
            Logger.LogInfo("Nameplates: " + Settings.Nameplates.Value
                + ", shared quest rewards: " + Settings.ShareQuestRewards.Value
                + ", shared silver: " + Settings.ShareSilver.Value
                + ", shared loot: " + Settings.ShareLoot.Value);

        }
    }
}
