# Expedition — Project Plan

An original co-op expansion for **Outward Definitive Edition**, inspired by Raid Mode:
bigger parties (4 players to start), co-op fixes, and **waymarks** (pings such as
"look here", "danger" or "loot") that the whole party sees.

Waymarks are the first feature and the learning project. They're small, but they
cover the key ideas in multiplayer programming: message design, serialization,
authority, ordering, late joiners and version mismatches. The ping code
(`PingBoard`, `PingUpdate`) keeps the waymark name inside Expedition.

## Ground rules for this project

- Nothing in the Outward install gets changed by the assistant. You install BepInEx,
  switch Steam branches, and copy DLLs yourself.
- Nothing gets downloaded (NuGet restore, tools, packages) or run (`dotnet build`,
  `dotnet test`, REA analysis) without your OK each time.
- You write the core logic. The assistant writes scaffolding, tests, and explanations.

## Reference documentation

| Source | What it gives us |
|---|---|
| [Outward Wiki: Mod development guide](https://outward.fandom.com/wiki/Mod_development_guide) | The main community reference. DE uses **Unity 2020.3.26**. It recommends BepInEx 5 + HarmonyX, the `Outward.GameLibs` NuGet package (publicized game assemblies), and lists the key classes: `Character`, `CharacterManager`, `SplitScreenManager`, `NetworkLevelLoader`, `ResourcesPrefabManager`, `Global`. |
| [Outward Mod Template](https://github.com/Mefino/OutwardModTemplate) | The starting point the wiki recommends for the plugin project. |
| [BepInEx 5 docs](https://docs.bepinex.dev/v5.4.11/) | Plugin lifecycle, config, logging. |
| [HarmonyX wiki](https://github.com/BepInEx/HarmonyX/wiki) | Patching game methods. |
| [Unity 2020.3 Scripting API](https://docs.unity3d.com/2020.3/Documentation/ScriptReference/) | Engine APIs. |
| [Thunderstore Outward](https://outward.thunderstore.io/) | Packaging and release (Phase 6). |

Note: the wiki says modding isn't maintained by Nine Dots Studio. The "official" docs
are community-run, so we check every claim about game internals against the real
assembly (see REA below).

## Development tools

| Tool | Purpose | Status on your machine |
|---|---|---|
| .NET SDK 8 | Build Core and run the tests | Installed (8.0.300) |
| IDE: Visual Studio Community (with the .NET desktop workload) or Rider | Editing and debugging | Not checked |
| Git | Version control, so you can diff your own progress | Not checked |
| BepInEx 5 for Outward (`BepInExPack_Outward` on Thunderstore) | Mod loader in the game | You install it in Phase 4 |
| NuGet feed `https://nuget.bepinex.dev/v3/index.json` | `Outward.GameLibs`, `BepInEx.BaseLib` 5.x, `HarmonyX` | Needed in Phase 4 only |
| xUnit (NuGet) | Unit tests for Core | Restored the first time you run tests (needs approval) |
| **REA** (MCP server, connected) | Read-only inspection of `Assembly-CSharp.dll` to learn how Outward's networking works | Ready |
| dnSpy / ILSpy (optional) | GUI decompiler, as the wiki suggests | Optional |
| UnityExplorer (optional) | Look at live game objects at runtime | Optional, Phase 4+ |
| r2modman (optional) | Separate mod profiles, so your main install stays clean | Recommended |

Game prerequisite (from the wiki and community guides): BepInEx mods need the
**Mono build** of Outward DE (Steam → Properties → Betas → *default-mono*). That
changes your install, so it's your decision and your action.

## Phases

Each phase has a learning goal and a definition of done.

### Phase 0: Setup (you are here)
- Read [ARCHITECTURE.md](ARCHITECTURE.md).
- Do [Exercise 01](exercises/01-sequence-ordering.md).
- Done when: all `PingBoardTests` pass on your machine.

### Phase 1: Learn how Outward does multiplayer (REA, read-only)
Questions to answer, each backed by evidence:
1. Which networking library does Outward use? (Community knowledge says Photon PUN.
   We verify that rather than assume it.)
2. How does the game send RPCs, and who is the authority (master client/host)?
3. How do we find the local player(s) and the remote player (`CharacterManager`,
   `SplitScreenManager`)?
4. When is it safe to start sending (after `NetworkLevelLoader` finishes loading)?

How: REA `inspect_managed_artifact` / `inspect_managed_members` on
`Outward_Data\Managed\Assembly-CSharp.dll`. This only reads the file and never
changes it. We record findings in `docs/findings.md`.
Done when: we can draw the send path from "player presses key" to "remote client receives".

### Phase 2: Core domain + protocol (no game needed)
- `PingBoard` (Exercise 01), `PingMessage` + binary codec, a protocol version handshake.
- Done when: round-trip codec tests and handshake tests pass.

### Phase 3: Simulated multiplayer
- `LoopbackNetwork` with deterministic latency, drop and reordering; `FakeClock`;
  host-authoritative `ExpeditionSession`.
- Scenario tests: two peers, a third joining late, duplicate packets, host leaving.
- Done when: the scenario suite passes with a fixed seed and with 100 random seeds.

### Phase 4: BepInEx plugin shell
- Create `Expedition.Plugin` from the Outward Mod Template; it references `Expedition.Core`.
- Plugin loads, logs, reads a config key, and does nothing else.
- Done when: the BepInEx log shows Expedition loaded (single player).

### Phase 5: Game adapters
- `PhotonTransport : ITransport` (or whatever Phase 1 finds), Unity input → command,
  world-space marker rendering, Harmony patches only where needed.
- Done when: two machines (or two Steam accounts) see each other's pings.

### Phase 6: Polish and release
- Config UI, mod version mismatch warning, Thunderstore package (`manifest.json`,
  README, icon), following the wiki's release section.

## Risks

| Risk | Mitigation |
|---|---|
| The networking API differs from what the community says | Phase 1 verifies it with evidence before any adapter code |
| Game updates break the Harmony patches | Keep patches small, in one folder, each with a comment naming the target method |
| The partner doesn't have the mod or has a different version | Protocol handshake (Phase 2); ignore unknown messages |
| Hard to debug in game | Most logic lives in Core and is covered by tests; the in-game layer stays thin |
