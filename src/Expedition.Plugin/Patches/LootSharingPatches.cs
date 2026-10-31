using System.Collections.Generic;
using HarmonyLib;

namespace Expedition.Plugin.Patches
{
    // How looting works in the game (read with REA):
    //   CharacterInventory.TakeItem(Item, bool)              - take one item (or a silver pile)
    //   CharacterInventory.TakeAllContent(ItemContainer, bool) - "Take all" from a chest or body
    //   CharacterInventory.AddMoney(int, int, bool)          - both of the above call this for silver
    // AddMoney already handles the network: on a guest it asks the host to add the silver.
    //
    // Shared silver: while a loot take is running, AddMoney splits the amount between players.
    // Shared loot:   when a player takes an item from a chest or body, every other player gets
    //                their own copy through ReceiveItemReward (the same network-safe path quests use).
    static class Loot
    {
        // How many loot takes are running right now (TakeAllContent calls TakeItem inside it).
        public static int ActiveTakes;

        // True while we hand out shares, so those AddMoney calls aren't split again.
        public static bool GivingShares;

        public static List<Character> Party()
        {
            var party = new List<Character>();
            CharacterManager manager = CharacterManager.Instance;
            if (manager == null)
            {
                return party;
            }

            for (int i = 0; i < manager.PlayerCharacters.Count; i++)
            {
                Character character = manager.GetCharacter(manager.PlayerCharacters.Values[i]);
                if (character != null)
                {
                    party.Add(character);
                }
            }
            return party;
        }

        // Only share things that come from the world: chests, enemy bodies, loose loot.
        // Never a player's own pouch, bag or stash, or splitting would take money from teammates.
        public static bool IsWorldLoot(ItemContainer source, List<Character> party)
        {
            if (source == null)
            {
                return true;
            }

            Character owner = source.OwnerCharacter;
            if (owner != null && !owner.IsAI)
            {
                return false;
            }

            foreach (Character player in party)
            {
                if (player.Stash == source)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsMoney(Item item)
        {
            return item is Currency
                || item.ItemID == Currency.SILVER_ITEMID
                || item.ItemID == Currency.GOLD_ITEMID;
        }
    }

    [HarmonyPatch(typeof(CharacterInventory), "TakeItem", new[] { typeof(Item), typeof(bool) })]
    static class TakeItemPatch
    {
        static void Prefix(CharacterInventory __instance, Item __0, out bool __state)
        {
            __state = false;
            Item item = __0;
            if (item == null)
            {
                return;
            }

            List<Character> party = Loot.Party();
            if (party.Count < 2 || !Loot.IsWorldLoot(item.ParentContainer, party))
            {
                return;
            }

            __state = true;
            Loot.ActiveTakes++;

            // Hand out copies only on the looter's own PC, so it happens exactly once.
            // Only items from a chest or body: an item lying on the ground may have been
            // dropped by a player, and copying it would duplicate their gear.
            Character looter = __instance.m_character;
            if (!Settings.ShareLoot.Value || Loot.IsMoney(item) || item.ParentContainer == null
                || looter == null || !looter.IsLocalPlayer)
            {
                return;
            }

            int quantity = item.RemainingAmount > 0 ? item.RemainingAmount : 1;
            foreach (Character player in party)
            {
                if (player != looter)
                {
                    player.Inventory.ReceiveItemReward(item.ItemID, quantity, false);
                }
            }
        }

        // A Finalizer runs even if the game's method throws, so the counter can't get stuck.
        static void Finalizer(bool __state)
        {
            if (__state)
            {
                Loot.ActiveTakes--;
            }
        }
    }

    [HarmonyPatch(typeof(CharacterInventory), "TakeAllContent")]
    static class TakeAllContentPatch
    {
        static void Prefix(ItemContainer __0, out bool __state)
        {
            List<Character> party = Loot.Party();
            __state = party.Count >= 2 && Loot.IsWorldLoot(__0, party);
            if (__state)
            {
                Loot.ActiveTakes++;
            }
        }

        static void Finalizer(bool __state)
        {
            if (__state)
            {
                Loot.ActiveTakes--;
            }
        }
    }

    [HarmonyPatch(typeof(CharacterInventory), "AddMoney", new[] { typeof(int), typeof(int), typeof(bool) })]
    static class AddMoneyPatch
    {
        // __0 is the silver amount (the first parameter). "ref" lets us change it.
        static void Prefix(CharacterInventory __instance, ref int __0)
        {
            if (!Settings.ShareSilver.Value || Loot.ActiveTakes == 0 || Loot.GivingShares || __0 <= 0)
            {
                return;
            }

            List<Character> party = Loot.Party();
            Character looter = __instance.m_character;
            int share = __0 / party.Count;
            if (party.Count < 2 || share == 0 || looter == null)
            {
                return;
            }

            Loot.GivingShares = true;
            try
            {
                foreach (Character player in party)
                {
                    if (player != looter)
                    {
                        player.Inventory.AddMoney(share);
                    }
                }
            }
            finally
            {
                Loot.GivingShares = false;
            }

            // The looter keeps their share plus anything that doesn't divide evenly.
            __0 -= share * (party.Count - 1);
        }
    }
}
