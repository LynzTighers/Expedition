# Expedition — Architecture

## The core idea: keep the game at arm's length

Code that touches `UnityEngine`, BepInEx, Harmony or Photon only runs inside the game.
If our multiplayer logic depends on those, the only way to test it is to launch two
copies of Outward. That's slow, hard to repeat, and impossible to automate.

So the code is split into two parts that depend on each other in **one direction only**:

```
┌──────────────────────────── Expedition.Plugin (net472, game only) ───────────────────────────┐
│  Plugin.cs (BepInEx entry)   InputAdapter   MarkerRenderer   PhotonTransport   Patches/   │
│        │                          │               ▲                 │                     │
│        └──────────── calls ───────┴─── reads ─────┴─── implements ──┘                     │
└──────────────────────────────────────────┬────────────────────────────────────────────────┘
                                           │ references
┌──────────────────────────── Expedition.Core (netstandard2.0, pure C#) ───────────────────────┐
│  Domain:    PingUpdate, PingKind, Position, PingBoard                                     │
│  Protocol:  PingMessage, MessageCodec, ProtocolVersion                                    │
│  Session:   ExpeditionSession (host-authoritative rules)                                     │
│  Ports:     ITransport, IClock, ILog          ◄── interfaces only, no implementation      │
└──────────────────────────────────────────┬────────────────────────────────────────────────┘
                                           │ referenced by
┌──────────────────────────── Expedition.Core.Tests (net8.0, xUnit) ───────────────────────────┐
│  Unit tests       LoopbackNetwork : ITransport       FakeClock : IClock                   │
└───────────────────────────────────────────────────────────────────────────────────────────┘
```

**Rule:** `Expedition.Core` must never reference `UnityEngine`, `BepInEx`, `HarmonyLib` or
Photon. If you find yourself wanting `Vector3` in Core, use our own `Position` struct
and convert at the edge, in the plugin.

### Why `netstandard2.0` for Core?
Outward DE runs on Unity 2020.3's Mono runtime, which can load `netstandard2.0`
assemblies. The .NET 8 test runner can load them too. One DLL, two hosts.
We set `LangVersion` 9.0 for nicer syntax, but avoid features that need newer runtime
types: no `record`, no `init`, no default interface methods.

## Ports and adapters

Core defines what it needs as interfaces ("ports"). Each host supplies its own
implementation ("adapter"):

| Port (in Core) | Game adapter (Plugin) | Test adapter (Tests) |
|---|---|---|
| `ITransport`: send bytes to a peer, raise an event on receive | Photon RPC / raise-event wrapper (Phase 5, after REA) | `LoopbackNetwork`: in-memory queues with seeded latency, drop and reordering |
| `IClock`: current time in ms | `UnityEngine.Time` | `FakeClock`: you advance it by hand |
| `ILog` | BepInEx `ManualLogSource` | Writes to a list the test can inspect |

Because `ExpeditionSession` only talks to these interfaces, a test can create **two or three
sessions in one process**, connect them through `LoopbackNetwork`, and play out a whole
co-op scene in a few milliseconds:

```csharp
var net   = new LoopbackNetwork(seed: 42, latencyMs: 80, dropRate: 0.1);
var clock = new FakeClock();
var host  = new ExpeditionSession(net.CreatePeer(1), clock, isHost: true);
var guest = new ExpeditionSession(net.CreatePeer(2), clock, isHost: false);

guest.PlacePing(PingKind.Danger, new Position(10, 0, 5));
net.RunUntilIdle(clock);                 // deliver, drop and reorder deterministically

Assert.True(host.Board.TryGet(2, out var ping));
Assert.Equal(PingKind.Danger, ping.Kind);
```

## Multiplayer rules we'll implement (and test) in Core

1. **Each ping update carries a per-player sequence number.** Networks duplicate and
   reorder messages, so receivers keep only the newest. *(Exercise 01)*
2. **The host is the authority.** Guests send *requests*; the host validates them
   (rate limit, valid kind) and broadcasts the accepted *state*. Everyone, including the
   host, applies the same broadcast, so all boards end up identical.
3. **Late joiners get a snapshot.** A new peer receives the host's full board, then
   live updates.
4. **Versioned protocol.** The first byte of every message is the protocol version.
   Unknown versions are logged and ignored, never thrown.
5. **Core never throws on bad input from the network.** Malformed bytes are rejected
   and counted.

## How the plugin stays thin

The plugin's job is translation only:
- key press → `session.PlacePing(kind, ToPosition(player.transform.position))`
- `session.Board` changed → create/move/destroy marker GameObjects
- Photon message received → `transport.Deliver(bytes)` into Core

If a plugin method grows an `if` about game rules, that logic probably belongs in Core,
where it can be tested.

## Folder layout

```
Expedition/
  docs/                      plan, architecture, exercises, REA findings
  src/Expedition.Core/          pure C# logic (you write most of this)
  src/Expedition.Plugin/        BepInEx plugin (Phase 4, from the Outward Mod Template)
  tests/Expedition.Core.Tests/  xUnit tests + simulation harness
```
