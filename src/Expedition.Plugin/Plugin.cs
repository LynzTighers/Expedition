using BepInEx;
using HarmonyLib;


namespace Expedition.Plugin
{

    [BepInPlugin("com.lynztighers.expedition", "Expedition", "0.1.0")]
    public class Plugin : BaseUnityPlugin

    {
        private void Awake()
        {
            Logger.LogInfo("THis mod has loaded!");

            // Applies every [HarmonyPatch] class in this mod (see the Patches folder).
            new Harmony("com.lynztighers.expedition").PatchAll();
            Logger.LogInfo("Room limit raised to " + Patches.RoomLimit.MaxPlayers + " players.");

        }
    }
}
