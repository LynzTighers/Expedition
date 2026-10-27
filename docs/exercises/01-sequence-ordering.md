# Exercise 01: Handling messages that arrive out of order

**Concept:** Networks deliver messages late, twice, or out of order. Every peer has to
reach the same state anyway. The standard fix is a **sequence number** for each sender:
keep the newest and ignore the rest.

**File you edit:** `src/Expedition.Core/Domain/PingBoard.cs`
**Tests that check you:** `tests/Expedition.Core.Tests/PingBoardTests.cs` (don't edit these)

## Task

Implement `PingBoard.TryApply(PingUpdate update)`:

1. If we've already seen an update from `update.PlayerId` with a sequence **greater than
   or equal to** `update.Sequence`, return `false` and change nothing.
2. Otherwise, remember this sequence as the newest for that player, and then:
   - if `update.Kind == PingKind.Clear`, remove the player's ping from `_active`;
   - otherwise store `update` in `_active`.
3. Return `true`.

## Run the tests

From the `Expedition` folder:

```bash
dotnet test tests/Expedition.Core.Tests
```

The first run downloads xUnit from nuget.org (a few MB). Before you
implement anything, every test should fail with `NotImplementedException`. That's
expected, and it proves the tests really run your code.

## Hints (read one at a time, only if stuck)

<details><summary>Hint 1</summary>

Look at `Stale_update_after_clear_does_not_resurrect_the_ping`. After a Clear, the player
isn't in `_active` any more. So how would you know that #1 is older than the Clear you
already applied?
</details>

<details><summary>Hint 2</summary>

Add a second dictionary, `Dictionary<int, int> _lastSequence`, that is **never** cleaned up
when a ping is cleared. Distributed-systems people call this keeping a "tombstone".
</details>

<details><summary>Hint 3</summary>

`_lastSequence.TryGetValue(update.PlayerId, out var last) && update.Sequence <= last` is
the "stale or duplicate" check.
</details>

## Questions to answer (write your answers below; we'll discuss them)

1. Why does `Players_are_independent` exist? What bug would a single global sequence counter cause?
2. `Same_updates_in_any_order_give_the_same_board` passes if your logic is right. Why
   does that matter for two players who each receive messages in a different order?
3. `Sequence` is an `int`. What happens after 2,147,483,647 pings? Is that a real risk for
   this mod? (Stretch: look up "serial number arithmetic", RFC 1982.)
4. Who should *assign* sequence numbers: the player who pings, or the host? Think about
   it with rule 2 from ARCHITECTURE.md (host authority) in mind.

### My answers

1.
2.
3.
4.
