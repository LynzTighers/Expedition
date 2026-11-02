using System.Collections.Generic;
using UnityEngine;

namespace Expedition.Plugin
{
    // Puts every player on the big map, using the game's own map markers.
    //
    // What REA showed: anything with a MapWorldMarker component appears on the map. Its
    // OnEnable calls the static MapDisplay.RegisterMapMarker, and MapDisplay then draws it with
    // a circle and a text label (MapWorldMarker.Text). So we only need to attach a
    // MapWorldMarker to each player's character; the game positions and draws it.
    // PartyMapTag carries the player's colour so MapMarkerPatches can tint the circle.
    class PartyMap : MonoBehaviour
    {
        const float RefreshSeconds = 1f;
        const string MarkerName = "ExpeditionMapMarker";

        float nextRefresh;

        public static void Create()
        {
            var obj = new GameObject("ExpeditionPartyMap");
            DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            obj.AddComponent<PartyMap>();
        }

        // Adds a game map marker under a world object, or returns the one already there.
        public static MapWorldMarker AttachMarker(Transform parent, string text, Color color)
        {
            Transform existing = parent.Find(MarkerName);
            GameObject holder;
            if (existing != null)
            {
                holder = existing.gameObject;
            }
            else
            {
                holder = new GameObject(MarkerName);
                holder.transform.SetParent(parent, false);
                // Inactive while we set it up, so the map never registers a half-made marker.
                holder.SetActive(false);
                holder.AddComponent<PartyMapTag>();
                MapWorldMarker created = holder.AddComponent<MapWorldMarker>();
                created.ShowCircle = true;
                created.ShowBackground = false;
            }

            MapWorldMarker marker = holder.GetComponent<MapWorldMarker>();
            marker.Text = text;
            holder.GetComponent<PartyMapTag>().Color = color;
            return marker;
        }

        void Update()
        {
            if (Time.time < nextRefresh)
            {
                return;
            }
            nextRefresh = Time.time + RefreshSeconds;

            CharacterManager manager = CharacterManager.Instance;
            if (manager == null)
            {
                return;
            }

            for (int i = 0; i < manager.PlayerCharacters.Count; i++)
            {
                Character player = manager.GetCharacter(manager.PlayerCharacters.Values[i]);
                if (player == null)
                {
                    continue;
                }

                bool show = Settings.MapMarkers.Value && (Settings.ShowSelfOnMap.Value || !player.IsLocalPlayer);
                MapWorldMarker marker = AttachMarker(player.transform, player.Name, PartyColors.ForCharacter(player));
                if (marker.gameObject.activeSelf != show)
                {
                    marker.gameObject.SetActive(show);
                }
            }
        }
    }

    // Marks a map marker as ours and remembers its colour.
    class PartyMapTag : MonoBehaviour
    {
        public Color Color = Color.white;
    }
}
