namespace Expedition.Core.Domain
{
    public enum PingKind : byte
    {
        /// <summary>Removes the player's current ping.</summary>
        Clear = 0,
        Look = 1,
        Danger = 2,
        Loot = 3,
    }

    /// <summary>
    /// One player's ping state at a point in time. Each player has at most one active ping;
    /// a newer update from the same player replaces the older one.
    /// </summary>
    public readonly struct PingUpdate
    {
        public PingUpdate(int playerId, int sequence, PingKind kind, Position position)
        {
            PlayerId = playerId;
            Sequence = sequence;
            Kind = kind;
            Position = position;
        }

        public int PlayerId { get; }

        /// <summary>
        /// Increases by one each time this player changes their ping. Lets receivers
        /// tell new updates from late or duplicated ones.
        /// </summary>
        public int Sequence { get; }

        public PingKind Kind { get; }
        public Position Position { get; }

        public override string ToString() => $"P{PlayerId}#{Sequence} {Kind} {Position}";
    }
}
