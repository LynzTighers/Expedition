# Expedition

A co-op expansion for **Outward Definitive Edition** (BepInEx 5, C#): bigger parties
(4 players to start), better co-op sync, and waymarks (pings) your party can see.

### ⬇️ [Download the latest version](https://github.com/LynzTighers/Expedition/releases/latest)

Open the link, scroll to **Assets**, and download `Expedition-x.y.z.zip`
(not "Source code"). Then follow [Install](#install) below.

> **Status: alpha. Tested with 3 players; not yet with 4.** Expect some bugs when more than
> 2 people play together. Please report what happens (see [Reporting problems](#reporting-problems)).

## What it does right now

| Feature | Status |
|---|---|
| Online rooms hold **4 players** instead of 2 | ✅ Works: tested with 3 players. Not yet tested with 4. |
| Waymarks (pings) | In development |

### 3-player test results

| What | Result |
|---|---|
| Third player joins (no stuck loading screen) | ✅ Works |
| Everyone sees each other move and fight | ✅ Works |
| Dropped items show up for everyone | ✅ Works |
| Going down and being revived | ✅ Works |
| No errors in the log | ✅ Works |
| Compass markers | ⚠️ Bug in 0.1.0: the first two players didn't see the third on their compass. Fixed in 0.1.1, not yet re-tested |
| Quest progress for every player | ❔ Not tested yet |

Normal single-player and 2-player co-op play as usual.

---

## Install

Everyone in the group should install it, including the host.

### 1. Switch Outward to the Mono version

BepInEx mods only work on this version.

1. In Steam, right-click **Outward** → **Properties** → **Betas**.
2. Choose **default-mono**.
3. Let Steam finish updating.

### 2. Install BepInEx

1. Download **BepInExPack_Outward** from
   [Thunderstore](https://thunderstore.io/c/outward/p/BepInEx/BepInExPack_Outward/)
   (click **Download** on the right).
2. Unzip it **somewhere else first**, such as your Downloads folder. Not into the game folder.
3. Find your game folder: in Steam, right-click **Outward** → **Manage** → **Browse local files**.
   Open the **`Outward_Defed`** folder. It contains `Outward Definitive Edition.exe`.
4. Open the unzipped **`BepInExPack_Outward`** folder and move **what's inside it** into
   `Outward_Defed`: the `BepInEx` folder, `winhttp.dll` and `doorstop_config.ini`.
   Don't move the `BepInExPack_Outward` folder itself.

   When you're done it should look like this:

   ```
   Outward_Defed\
     Outward Definitive Edition.exe
     winhttp.dll
     doorstop_config.ini
     BepInEx\
   ```

   ⚠️ Use `Outward_Defed`, not the outer `Outward` folder with the classic `Outward.exe`.
   If BepInEx is there, it won't load in the Definitive Edition.
5. Start the game once, then close it. This creates `Outward_Defed\BepInEx\plugins`.

### 3. Install Expedition

1. Download the latest `Expedition-x.y.z.zip` from the
   [latest release](https://github.com/LynzTighers/Expedition/releases/latest), under **Assets**.
2. Copy **both** DLLs into `Outward_Defed\BepInEx\plugins\`:
   - `Expedition.Plugin.dll`
   - `Expedition.Core.dll`
3. Remove other co-op mods such as **Raid Mode**. They change the same things.

### 4. Check that it loaded

Open `Outward_Defed\BepInEx\LogOutput.log` with Notepad. You should see:

```
[Info   :   BepInEx] Loading [Expedition 0.1.1]
[Info   :Expedition] THis mod has loaded!
[Info   :Expedition] Room limit raised to 4 players.
```

If there's no `LogOutput.log` at all, BepInEx isn't loading: re-check step 2.

## Play together

1. The host loads their character and hosts an online game as usual.
2. Everyone else joins it as usual.
3. Everyone should use the **same Expedition version**.

The game's menus still show 2 players. That's expected: the limit is raised behind the
scenes, and the menus haven't been updated yet.

## Uninstall

Delete `Expedition.Plugin.dll` and `Expedition.Core.dll` from `BepInEx\plugins\`.

## Reporting problems

Open an issue, or message the author, with:

- how many players were in the game, and who was the host
- what happened, and what you expected
- each player's `BepInEx\LogOutput.log` from that session, especially red error lines

---

## For developers

See [How the source code works](src/README.md).

```
src/Expedition.Core/          pure C# logic, no Unity (netstandard2.0)
tests/Expedition.Core.Tests/  xUnit tests (net8.0)
src/Expedition.Plugin/        BepInEx plugin that runs inside Outward
```

Build the plugin (needs the .NET SDK; packages come from nuget.org and the BepInEx feed,
see `NuGet.Config`):

```bash
dotnet build src/Expedition.Plugin/Expedition.Plugin.csproj
```

The DLLs land in `src/Expedition.Plugin/bin/Debug/net472/`.

## Credits

Inspired by [Raid Mode](https://www.nexusmods.com/outward/mods/183) by SpicerXD.

Developed with the help of AI coding tools (Claude Code), used for research,
explanations and code review while I learned C# and Outward modding.

## License

[MIT](LICENSE)
