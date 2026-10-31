using HarmonyLib;

namespace Expedition.Plugin.Patches
{
    // OtherPlayersCompass.Update (read with REA) fills a list of "players to show on my compass":
    //
    //   slot = 0
    //   for each player in the lobby (except me):
    //       if (watched[slot] == null) { watched[slot] = player; slot++; }
    //
    // The slot only moves forward when it was empty. Once slot 0 holds player 2, it never
    // moves past it, so a player who joins later is never added. Players 1 and 2 filled
    // their list before player 3 arrived, so they never see player 3. Player 3 joined last,
    // so their list started empty and filled with everyone.
    //
    // Fix: empty the list just before the game's Update runs, so it refills every player
    // in order each frame. The game's own code then does the rest.
    [HarmonyPatch(typeof(OtherPlayersCompass), "Update")]
    static class OtherPlayersCompassPatch
    {
        static void Prefix(OtherPlayersCompass __instance)
        {
            var watched = __instance.m_watchedElements;
            if (watched == null)
            {
                return;
            }

            for (int i = 0; i < watched.Length; i++)
            {
                watched[i] = null;
            }
        }
    }
}
