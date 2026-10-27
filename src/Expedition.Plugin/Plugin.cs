using BepInEx;
using HarmonyLib;
using UnityEngine;


namespace Expedition.Plugin
{

    [BepInPlugin("com.lynztighers.expedition", "Expedition", "0.1.0")]
    public class Plugin : BaseUnityPlugin

    {
        bool hasDisplayed = false;
        // -1 means "no timer running yet".
        float showAt = -1f;
        private void Awake()
        {
            Logger.LogInfo("THis mod has loaded!");

            // Applies every [HarmonyPatch] class in this mod (see the Patches folder).
            new Harmony("com.lynztighers.expedition").PatchAll();
            Logger.LogInfo("Room limit raised to " + Patches.RoomLimit.MaxPlayers + " players.");

        }
        private void Update()
        {
            if (NetworkLevelLoader.Instance != null && NetworkLevelLoader.Instance.IsOverallLoadingDone)
            {
                if (CharacterManager.Instance != null && CharacterManager.Instance.GetWorldHostCharacter() != null
                 && !hasDisplayed && CharacterManager.Instance.GetWorldHostCharacter().CharacterUI != null)
                {
                    // Wait 3 seconds after loading so the screen has finished fading in.
                    if (showAt < 0f)
                    {
                        showAt = Time.time + 3f;
                    }
                    else if (Time.time >= showAt)
                    {
                        hasDisplayed = true;
                        CharacterManager.Instance.GetWorldHostCharacter().CharacterUI.ShowInfoNotification("HELLO YOU PIECE OF G33k !!!");
                    }
                }

            }
        }
    }
}
