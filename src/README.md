# How the Expedition source code works

This folder holds the two projects that become the mod. Read this from top to bottom
the first time; after that, jump to the section you need.

```
src/
  Expedition.Core/              pure C# rules, no Unity or game code (tested without the game)
    Domain/
      Position.cs            an X, Y, Z point
      PingUpdate.cs          one "player N pinged here" message
      PingBoard.cs           keeps the newest ping per player (Exercise 01)
  Expedition.Plugin/            the BepInEx mod that runs inside Outward
    Plugin.cs                the entry point: loads the mod, turns on patches, shows the welcome message
    Patches/
      PhotonRoomPatches.cs   raises the online player limit from 2 to 4
```

Why two projects? Code in `Expedition.Core` can be tested in a second with
`dotnet test`, without starting Outward. `Expedition.Plugin` stays thin: it connects
the game to Core. See `docs/ARCHITECTURE.md` for the longer version.

---

## Plugin.cs: the mod's front door

BepInEx looks for a class that inherits `BaseUnityPlugin` and has a `[BepInPlugin]`
attribute. That class is `Plugin`.

| Part | What it does |
|---|---|
| `[BepInPlugin("com.lynztighers.expedition", "Expedition", "0.1.0")]` | The mod's unique ID, name and version. |
| `Awake()` | Unity calls this once when the mod loads. It writes to the log and turns on Harmony patches. |
| `new Harmony("com.lynztighers.expedition").PatchAll()` | Finds every class marked `[HarmonyPatch]` in this mod and applies it. |
| `Update()` | Unity calls this every frame. It shows the welcome message once you're in the world. |

### The welcome message, step by step

1. Wait until `NetworkLevelLoader.Instance.IsOverallLoadingDone` is `true` (loading has finished).
2. Get the host's character: `CharacterManager.Instance.GetWorldHostCharacter()`.
3. Start a 3-second timer (`showAt = Time.time + 3f`). Showing the message straight away
   didn't work: the screen was still fading in and the popup vanished.
4. When the timer runs out, call `CharacterUI.ShowInfoNotification(...)` and set
   `hasDisplayed = true` so it only shows once per game launch.

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
3. **UI built for 2.** Compass markers, rest menu markers and respawn points.
4. **Balance.** Enemy scaling, revives, rewards.

### Testing it

- Only the **host** needs this patch for the room to accept 4 players, because Photon
  enforces the limit the room was created with.
- Every player should still install Expedition, because the fixes in the list above will
  need to run on everyone's game.
- Check `BepInEx\LogOutput.log` for `Room limit raised to 4 players.` If Harmony can't find
  a method, it logs an error naming the patch.
- Split-screen is a separate system, and this patch doesn't change it.
