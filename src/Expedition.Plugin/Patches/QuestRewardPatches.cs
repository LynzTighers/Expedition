using HarmonyLib;
using NodeCanvas.Tasks.Actions;

namespace Expedition.Plugin.Patches
{
    // Quests give rewards with a GiveReward step. Each one has a RewardReceiver setting
    // (read with REA): Host, Instigator (whoever triggered it) or Everyone.
    // With Everyone, the game's own code already loops over every player and sends each
    // their reward over the network (CharacterInventory.ReceiveItemReward / ReceiveMoneyReward /
    // ReceiveSkillReward, plus XP). So we only switch the setting to Everyone while the
    // step runs, then put it back.
    [HarmonyPatch(typeof(GiveReward), "OnExecute")]
    static class QuestRewardPatch
    {
        static void Prefix(GiveReward __instance, out GiveReward.Receiver __state)
        {
            __state = __instance.RewardReceiver;

            // Quests run on the host. If this ever runs on a guest, leave it alone so
            // rewards can't be handed out twice.
            if (!Settings.ShareQuestRewards.Value || PhotonNetwork.isNonMasterClientInRoom)
            {
                return;
            }

            __instance.RewardReceiver = GiveReward.Receiver.Everyone;
        }

        static void Postfix(GiveReward __instance, GiveReward.Receiver __state)
        {
            __instance.RewardReceiver = __state;
        }
    }
}
