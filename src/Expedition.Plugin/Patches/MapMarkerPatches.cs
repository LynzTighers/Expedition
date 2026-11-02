using HarmonyLib;

namespace Expedition.Plugin.Patches
{
    // MapWorldMarkerDisplay.UpdateMarkerDisplay(MapWorldMarker) draws one marker on the big map
    // and sets its colours itself (read with REA). After it runs, recolour ours: the circle
    // gets the player's colour, and the label a darker shade so it stays readable on the map.
    [HarmonyPatch(typeof(MapWorldMarkerDisplay), "UpdateMarkerDisplay")]
    static class MapMarkerColorPatch
    {
        static void Postfix(MapWorldMarkerDisplay __instance, MapWorldMarker __0)
        {
            if (__0 == null)
            {
                return;
            }

            PartyMapTag tag = __0.GetComponent<PartyMapTag>();
            if (tag == null)
            {
                return;
            }

            if (__instance.Circle != null)
            {
                __instance.Circle.color = tag.Color;
            }
            if (__instance.Text != null)
            {
                __instance.Text.color = PartyColors.Darken(tag.Color);
            }
        }
    }
}
