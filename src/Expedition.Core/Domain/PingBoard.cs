using System;
using System.Collections.Generic;

namespace Expedition.Core.Domain
{
    /// <summary>
    /// Every peer's view of the active pings. Updates may arrive late, duplicated or out of
    /// order; the board must still end up in the same state on every peer.
    /// </summary>
    public sealed class PingBoard
    {
        // Active pings, one per player.
        private readonly Dictionary<int, PingUpdate> _active = new Dictionary<int, PingUpdate>();

        // EXERCISE 01: you'll likely need another field here.

        public int Count => _active.Count;

        public bool TryGet(int playerId, out PingUpdate ping) => _active.TryGetValue(playerId, out ping);

        /// <summary>
        /// Applies an update if it's newer than anything already seen from that player.
        /// </summary>
        /// <returns>true if the board changed its knowledge; false if the update was stale or a duplicate.</returns>
        public bool TryApply(PingUpdate update)
        {
            // EXERCISE 01: implement me.
            throw new NotImplementedException("Exercise 01");
        }
    }
}
