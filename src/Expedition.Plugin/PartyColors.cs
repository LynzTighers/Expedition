using UnityEngine;

namespace Expedition.Plugin
{
    // Each player gets one colour, used for their pings and their marker on the big map.
    // The colours are muted and warm to sit with Outward's parchment-and-gold UI.
    //
    // The colour comes from the player's Photon actor number (1 for the host, then 2, 3, 4
    // in join order). Every PC agrees on that number, so everyone sees the same colour
    // for the same player.
    static class PartyColors
    {
        static readonly Color[] Palette =
        {
            new Color(0.89f, 0.70f, 0.36f), // gold     #E3B25C
            new Color(0.40f, 0.72f, 0.68f), // teal     #66B8AD
            new Color(0.85f, 0.44f, 0.35f), // rust     #D97059
            new Color(0.62f, 0.53f, 0.84f), // lavender #9E87D6
        };

        // Parchment, for text drawn over the game world.
        public static readonly Color Parchment = new Color(0.93f, 0.87f, 0.75f);
        public static readonly Color ParchmentDim = new Color(0.78f, 0.71f, 0.58f);

        public static Color ForActor(int actorNumber)
        {
            int index = actorNumber <= 0 ? 0 : (actorNumber - 1) % Palette.Length;
            return Palette[index];
        }

        public static Color ForCharacter(Character character)
        {
            return character != null && character.photonView != null
                ? ForActor(character.photonView.ownerId)
                : Palette[0];
        }

        // A darker shade, for text drawn on the light map background.
        public static Color Darken(Color color)
        {
            return new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
        }
    }
}
