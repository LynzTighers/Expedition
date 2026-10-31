# How the Expedition source code works

This folder holds the two projects that become the mod. Read this from top to bottom
the first time; after that, jump to the section you need.

```
src/
  Expedition.Core/           pure C# rules, no Unity or game code (tested without the game)
    Domain/
      Position.cs            an X, Y, Z point
      PingUpdate.cs          one "player N pinged here" message
      PingBoard.cs           keeps the newest ping per player (in progress)
  Expedition.Plugin/         the BepInEx mod that runs inside Outward
    Plugin.cs                the entry point: loads settings and turns on patches
    Settings.cs              on/off switches, saved in BepInEx\config
    Pings.cs                 middle-click to ping a spot that every player sees
    Patches/
      PhotonRoomPatches.cs   raises the online player limit from 2 to 4
      CompassPatches.cs      shows every other player on the compass, not just the first
      NameplatePatches.cs    other players' names on their health bars, at any distance
      QuestRewardPatches.cs  quest rewards go to every player
      LootSharingPatches.cs  silver is split, chest and enemy items are copied to everyone
```

Why two projects? Code in `Expedition.Core` can be tested in a second with
`dotnet test`, without starting Outward. `Expedition.Plugin` stays thin: it connects
the game to Core.

---

## Plugin.cs: the mod's front door

BepInEx looks for a class that inherits `BaseUnityPlugin` and has a `[BepInPlugin]`
attribute. That class is `Plugin`.

| Part | What it does |
|---|---|
| `[BepInPlugin("com.lynztighers.expedition", "Expedition", "0.1.0")]` | The mod's unique ID, name and version. |
| `Awake()` | Unity calls this once when the mod loads. It writes to the log and turns on Harmony patches. |
| `new Harmony("com.lynztighers.expedition").PatchAll()` | Finds every class marked `[HarmonyPatch]` in this mod and applies it. |

---

## Harmony in 60 seconds

Harmony lets a mod run its own code **before** or **after** one of the game's methods,
without changing the game's files on disk. The changes only exist in memory while the game runs.

- **Prefix** runs *before* the game's method. It can change the arguments the method receives.
- **Postfix** runs *after* it. It can read or change the result.
- `[HarmonyPatch(typeof(SomeClass), "MethodName", new[] { ...parameter types... })]`
  names the method to patch. The list of types picks the right version when a method
  has several (overloads).

Special parameter names Harmony understands:

| Name | Meaning |
|---|---|
| `__instance` | the object the method was called on |
| `__result` | the method's return value (Postfix, or Prefix with `ref`) |
| `__0`, `__1`, ... | the method's parameters by position, counting from 0 |
| a real parameter name | that parameter, matched by its exact name |

---

## Patches/PhotonRoomPatches.cs: 4 players instead of 2

### What the game does (found with REA)

Outward uses **Photon** for online play. When you host, the game creates a Photon
"room" and tells it how many players may join. REA showed two places that do this,
and both hard-code **2**:

| Game method | Calls | Sets |
|---|---|---|
| `ConnectPhotonMaster.CreateRoom` | `PhotonNetwork.CreateRoom(name, options, lobby)` | `MaxPlayers = 2` |
| `ConnectPhotonMaster.CreateOrJoin` | `PhotonNetwork.JoinOrCreateRoom(name, options, lobby)` | `maxPlayers = 2` |

Raid Mode patches both too. Patching only the first would leave a way to
create a 2-player room.

### What the patch does

Two small Prefixes, one for each Photon method. Each one receives the `RoomOptions`
just before Photon sees it and raises `MaxPlayers` to 4:

```
game builds RoomOptions (MaxPlayers = 2)
        |
        v
  our Prefix:  MaxPlayers = 4        <- RoomLimit.Raise(...)
        |
        v
Photon creates the room with room for 4
```

- `RoomLimit.MaxPlayers` is the one number to change if you want to try 6 or 8.
- `Raise` only ever *increases* the limit, and does nothing if the options are `null`.
- `__1` is the 2nd parameter (`RoomOptions`). Matching by position means the patch
  doesn't depend on Photon's parameter names.

### How this differs from Raid Mode

Raid Mode **replaces** the whole `ConnectPhotonMaster.CreateRoom` method with its own copy
(a Prefix that returns `false`, so the original never runs). Expedition only changes the one
value and lets the game's own code run normally. Less copied code means less to break
if the game is updated.

### What this patch does **not** do yet

Raising the limit lets players 3 and 4 **into the room**. The rest of the game still
assumes 2 players, so expect problems. These are the next tasks, in order:

1. **Joining during loading screens.** The game waits for "all players" to finish loading,
   and that check can get stuck with extra players.
2. **Messages that only reach one player.** Some of the game's sync messages (items, stash,
   quests) are sent only to the host. With 2 players that's everyone else; with 4 it isn't.
3. **UI built for 2.** Rest menu markers and respawn points. (Compass markers: fixed, see below.)
4. **Balance.** Enemy scaling, revives, rewards.

### Testing it

- Only the **host** needs this patch for the room to accept 4 players, because Photon
  enforces the limit the room was created with.
- Every player should still install Expedition, because the fixes in the list above will
  need to run on everyone's game.
- Check `BepInEx\LogOutput.log` for `Room limit raised to 4 players.` If Harmony can't find
  a method, it logs an error naming the patch.
- Split-screen is a separate system, and this patch doesn't change it.

---

## Patches/CompassPatches.cs: every player on the compass

### The bug (found in a 3-player test)

The last player to join saw everyone on their compass, but the first two players
didn't see the third.

### Why (read from the game's code with REA)

`OtherPlayersCompass.Update` keeps an array called `m_watchedElements`: the players
your compass shows. Every frame it does this:

```
slot = 0
for each player in the lobby (except you):
    if (watched[slot] == null) {   // only if this slot is empty...
        watched[slot] = player;
        slot++;                    // ...does it move to the next slot
    }
```

The slot only moves forward when it was **empty**. Walk through it as player 1:

| When | What happens |
|---|---|
| Player 2 joins | Slot 0 is empty, so player 2 goes in slot 0. |
| Player 3 joins | The array grows to 2 slots. Player 2: slot 0 is full, so skip, and `slot` stays 0. Player 3: slot 0 is *still* full, so skip again. Player 3 is never added. |

Player 3 joined last, so their array started empty and filled with everyone. That
matches exactly what we saw.

### The fix

A **Prefix** empties the array just before the game's `Update` runs. The game's own
loop then refills it from scratch every frame, so every player gets a slot in order.
It's a few `null` writes per frame, so it costs nothing noticeable.

Raid Mode fixes this the same way, which is a good sign the approach is safe.

---

## Settings.cs: on/off switches

BepInEx gives every plugin a `Config` object. `config.Bind(section, key, default, description)`
creates a setting and saves it to `BepInEx\config\com.lynztighers.expedition.cfg`, so players
can turn features off with Notepad. `Settings.Bind(Config)` runs in `Awake()` **before**
`PatchAll()`, so the patches can read the values.

---

## Patches/NameplatePatches.cs: names over other players

Every character with a floating health bar has a `CharacterBarListener`. Two of its methods matter:

| Method | What it does normally | What the Postfix changes (other players only) |
|---|---|---|
| `UpdateDisplay` | Fills in the bars. A full health bar hides itself. | `HideIfFull = false`, and the bar's text label (`m_lblValue`) shows the player's name |
| `UpdateVisibility` | Hides the bar if the character is too far (`m_inRange`) or behind something (`m_inSight`) | Forces both to `true` and a huge `DisplayDistance`, then calls `UpdateSeenByCamera()` so the bar still only shows when the player is on your screen |

"Other player" means the bar's character has an `OwnerPlayerSys` (it belongs to a real
player) and isn't you.

---

## Patches/QuestRewardPatches.cs: rewards for everyone

Quests hand out rewards with a NodeCanvas task called `GiveReward`. REA showed it has a
`RewardReceiver` setting with three values: `Host`, `Instigator` (whoever triggered the
quest step) and `Everyone`. With `Everyone`, the game's own code already gives each player
their reward over the network.

So the patch is tiny:

- **Prefix:** remember the original setting in `__state`, then switch it to `Everyone`.
- **Postfix:** put the original setting back.

`__state` is a Harmony feature: whatever the Prefix stores there is handed to the Postfix.
The Prefix does nothing on a guest's PC (`PhotonNetwork.isNonMasterClientInRoom`), because
quests run on the host, and a second copy would give rewards twice.

---

## Patches/LootSharingPatches.cs: shared silver and loot

### What the game does (read with REA)

| Method | When | Calls |
|---|---|---|
| `CharacterInventory.TakeItem(Item, bool)` | Taking one item or a silver pile | `AddMoney` for silver (on the host) |
| `CharacterInventory.TakeAllContent(ItemContainer, bool)` | "Take all" on a chest or body | `TakeItem` for the items, `AddMoney` for the silver |
| `CharacterInventory.AddMoney(int, int, bool)` | Any time silver is added | On a guest, sends the request to the host |
| `CharacterInventory.ReceiveItemReward(id, qty, bool)` | Quest item rewards | Sends the item to that player's PC if it isn't yours |

### Shared silver

1. `TakeItem` and `TakeAllContent` get a **Prefix** that adds 1 to `Loot.ActiveTakes` when
   the source is world loot, and a **Finalizer** that subtracts it again. A Finalizer runs
   even if the game's method crashes, so the counter can't get stuck.
2. `AddMoney` gets a **Prefix**. If a loot take is running, it gives every other player
   `amount / players`, then lowers the looter's amount with `ref int __0` (the first
   parameter, by position).
3. `Loot.GivingShares` stops those extra `AddMoney` calls from being split again.

Silver is only split when it comes from the world. `IsWorldLoot` refuses containers owned
by a player (`OwnerCharacter` that isn't AI) and every player's stash. Otherwise taking
silver out of your own stash would hand it to your teammates.

### Shared loot

Inside the `TakeItem` Prefix, if the item comes from a chest or body, every other player
gets `ReceiveItemReward(item.ItemID, quantity, false)`. That's the same network-safe path
quest rewards use, and it shows them a "received" message.

Rules that stop duplicates:

- Copies are made only on the **looter's own PC** (`IsLocalPlayer`), so each take is copied once.
- Only items **inside a container**. A loose item on the ground might have been dropped by a
  player, and copying it would duplicate their gear.
- Silver and gold are never copied. They're split instead.

### Not checked yet

- Whether "Take all" goes through `TakeItem(Item, bool)` for every item. If it doesn't,
  single takes are copied but "Take all" isn't.
- A player's dropped backpack lying on the ground may count as world loot.

---

## Pings.cs: ping a spot for the party

This one isn't a Harmony patch. It's our own Unity component, created in `Awake()` with
`Pings.Create()`.

### Step by step

| Step | Where | What happens |
|---|---|---|
| 1. Press the key | `Update()` | Every frame it checks `Input.GetKeyDown(PingKey)`. A 0.5 s cooldown stops spam. |
| 2. Find the spot | `FindSpot()` | A ray goes from the middle of your screen up to 200 m. The first thing it hits that isn't your own character is the spot. If it hits nothing (the sky), the ping goes 10 m in front of you. |
| 3. Tell everyone | `photonView.RPC(nameof(ReceivePing), PhotonTargets.All, name, spot)` | Photon calls `ReceivePing` on **every** player's PC, yours included. Offline, it just shows the ping locally. |
| 4. Remember it | `ReceivePing()` → `Show()` | Saved in a dictionary keyed by the sender's Photon player ID, so each player has at most one ping. Other players also get a "*Name* pinged a spot" notification. |
| 5. Draw it | `OnGUI()` | Every frame, each ping's 3D position becomes a screen position with `WorldToScreenPoint`. If it's in front of the camera, the label `Name · 24 m ▼` is drawn there, with a black shadow so it's readable anywhere. Expired pings are removed. |

### How the networking works

Photon can only send an RPC through a `PhotonView`, an ID that every player's game agrees on.
Normally the game hands these out. We make our own object with a **fixed** ID, `981`, so it
exists with the same ID on every PC without anyone sending it. Raid Mode does the same with
`980`. The method that receives the message must have the `[PunRPC]` attribute.

### Settings

`Pings` (on/off), `PingKey` (a Unity `KeyCode`, default `Mouse2` = middle mouse) and
`PingSeconds` (default 20) live in the `[Party]` section of the config file.

### Not checked yet

- Whether Outward already uses the middle mouse button for something. If it does, change
  `PingKey`.
- The ping only shows while the spot is in front of your camera. There's no arrow at the
  screen edge for pings behind you yet.
