namespace Expedition.Core.Domain
{
    /// <summary>
    /// A world-space position. Our own type instead of UnityEngine.Vector3 so Core
    /// stays game-independent; the plugin converts at the edge.
    /// </summary>
    public readonly struct Position
    {
        public Position(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}
