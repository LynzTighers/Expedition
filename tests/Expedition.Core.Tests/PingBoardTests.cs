using Expedition.Core.Domain;
using Xunit;

namespace Expedition.Core.Tests
{
    public class PingBoardTests
    {
        private static readonly Position Here = new Position(1, 2, 3);
        private static readonly Position There = new Position(9, 8, 7);

        private static PingUpdate Ping(int player, int seq, PingKind kind = PingKind.Look, Position? pos = null)
            => new PingUpdate(player, seq, kind, pos ?? Here);

        [Fact]
        public void First_update_is_accepted()
        {
            var board = new PingBoard();

            Assert.True(board.TryApply(Ping(player: 1, seq: 1)));
            Assert.True(board.TryGet(1, out var ping));
            Assert.Equal(1, ping.Sequence);
        }

        [Fact]
        public void Newer_update_replaces_older()
        {
            var board = new PingBoard();
            board.TryApply(Ping(1, 1, PingKind.Look, Here));

            Assert.True(board.TryApply(Ping(1, 2, PingKind.Danger, There)));
            board.TryGet(1, out var ping);
            Assert.Equal(PingKind.Danger, ping.Kind);
            Assert.Equal(There.X, ping.Position.X);
        }

        [Fact]
        public void Older_update_arriving_late_is_ignored()
        {
            var board = new PingBoard();
            board.TryApply(Ping(1, 5, PingKind.Danger));

            Assert.False(board.TryApply(Ping(1, 3, PingKind.Loot)));
            board.TryGet(1, out var ping);
            Assert.Equal(PingKind.Danger, ping.Kind);
        }

        [Fact]
        public void Duplicate_update_is_ignored()
        {
            var board = new PingBoard();
            board.TryApply(Ping(1, 4));

            Assert.False(board.TryApply(Ping(1, 4)));
        }

        [Fact]
        public void Players_are_independent()
        {
            var board = new PingBoard();
            board.TryApply(Ping(1, 10));

            // Player 2's low sequence number must not be compared against player 1's.
            Assert.True(board.TryApply(Ping(2, 1)));
            Assert.Equal(2, board.Count);
        }

        [Fact]
        public void Clear_removes_the_ping()
        {
            var board = new PingBoard();
            board.TryApply(Ping(1, 1, PingKind.Danger));

            Assert.True(board.TryApply(Ping(1, 2, PingKind.Clear)));
            Assert.False(board.TryGet(1, out _));
            Assert.Equal(0, board.Count);
        }

        [Fact]
        public void Stale_update_after_clear_does_not_resurrect_the_ping()
        {
            // The network delivered these out of order: #2 (Clear) arrived before #1 (Danger).
            var board = new PingBoard();
            board.TryApply(Ping(1, 2, PingKind.Clear));

            Assert.False(board.TryApply(Ping(1, 1, PingKind.Danger)));
            Assert.False(board.TryGet(1, out _));
        }

        [Fact]
        public void Same_updates_in_any_order_give_the_same_board()
        {
            var updates = new[]
            {
                Ping(1, 1, PingKind.Look),
                Ping(1, 2, PingKind.Danger),
                Ping(2, 1, PingKind.Loot),
                Ping(1, 3, PingKind.Clear),
                Ping(2, 2, PingKind.Danger),
            };

            var inOrder = new PingBoard();
            foreach (var u in updates) inOrder.TryApply(u);

            var reversed = new PingBoard();
            for (int i = updates.Length - 1; i >= 0; i--) reversed.TryApply(updates[i]);

            Assert.Equal(inOrder.Count, reversed.Count);
            Assert.Equal(inOrder.TryGet(1, out _), reversed.TryGet(1, out _));
            inOrder.TryGet(2, out var a);
            reversed.TryGet(2, out var b);
            Assert.Equal(a.Sequence, b.Sequence);
            Assert.Equal(a.Kind, b.Kind);
        }
    }
}
