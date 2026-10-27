using HarmonyLib;

namespace Expedition.Plugin.Patches
{
    // Outward hard-codes 2 players in two places (found with REA):
    //   ConnectPhotonMaster.CreateRoom   -> PhotonNetwork.CreateRoom(...)
    //   ConnectPhotonMaster.CreateOrJoin -> PhotonNetwork.JoinOrCreateRoom(...)
    // Both build a RoomOptions with MaxPlayers = 2 and pass it to Photon.
    // These Prefixes run just before Photon gets the options and raise the cap.
    static class RoomLimit
    {
        public const byte MaxPlayers = 4;

        public static void Raise(RoomOptions roomOptions)
        {
            if (roomOptions != null && roomOptions.MaxPlayers < MaxPlayers)
            {
                roomOptions.MaxPlayers = MaxPlayers;
            }
        }
    }

    // __1 means "the method's 2nd parameter" (counting from 0), which is the RoomOptions.
    // Using the position instead of the name means the patch can't break over a parameter's name.
    [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.CreateRoom),
        new[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby) })]
    static class CreateRoomPatch
    {
        static void Prefix(RoomOptions __1)
        {
            RoomLimit.Raise(__1);
        }
    }

    [HarmonyPatch(typeof(PhotonNetwork), nameof(PhotonNetwork.JoinOrCreateRoom),
        new[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby) })]
    static class JoinOrCreateRoomPatch
    {
        static void Prefix(RoomOptions __1)
        {
            RoomLimit.Raise(__1);
        }
    }
}
