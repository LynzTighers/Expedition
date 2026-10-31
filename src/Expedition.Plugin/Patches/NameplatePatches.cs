using HarmonyLib;

namespace Expedition.Plugin.Patches
{
    // Each character on screen has a CharacterBarListener: the floating health bar.
    // Two of its methods decide what we see (both read with REA):
    //   UpdateDisplay    - fills in the bar; the bar can show a text label (m_lblValue)
    //   UpdateVisibility - hides the bar when the character is far away or out of sight
    // For other players we put their name in that label and keep the bar visible.
    static class Nameplates
    {
        // True for another player's bar (not your own, not an enemy or NPC).
        public static bool IsOtherPlayer(CharacterBarListener bar)
        {
            Character target = bar.TargetCharacter;
            return target != null
                && target.OwnerPlayerSys != null
                && target != bar.LocalCharacter;
        }
    }

    [HarmonyPatch(typeof(CharacterBarListener), "UpdateDisplay")]
    static class NameplateDisplayPatch
    {
        static void Postfix(CharacterBarListener __instance)
        {
            if (!Settings.Nameplates.Value || !Nameplates.IsOtherPlayer(__instance))
            {
                return;
            }

            Bar health = __instance.m_healthBar;
            if (health == null || health.m_lblValue == null)
            {
                return;
            }

            // Normally a full health bar hides itself. Keep it so the name stays readable.
            health.HideIfFull = false;
            health.m_lblValue.gameObject.SetActive(true);
            health.m_lblValue.text = __instance.TargetCharacter.Name;
        }
    }

    [HarmonyPatch(typeof(CharacterBarListener), "UpdateVisibility")]
    static class NameplateVisibilityPatch
    {
        static void Postfix(CharacterBarListener __instance)
        {
            if (!Settings.Nameplates.Value || !Nameplates.IsOtherPlayer(__instance))
            {
                return;
            }

            Character local = __instance.LocalCharacter;
            if (local == null || !local.IsStartInitDone || __instance.m_targetCharacterBarManager == null)
            {
                return;
            }

            // "In range" and "in sight" are what normally hide far-away bars. Force both on,
            // then let the game check whether the player is actually on screen.
            __instance.m_targetCharacterBarManager.DisplayDistance = 100000f;
            __instance.m_inRange = true;
            __instance.m_inSight = true;
            __instance.UpdateSeenByCamera();
        }
    }
}
