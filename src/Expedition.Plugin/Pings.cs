using System.Collections.Generic;
using UnityEngine;

namespace Expedition.Plugin
{
    // Press the ping key (G by default) to mark the spot in front of you.
    // Every player sees a marker there in your party colour, with your name and the distance,
    // and the ping also shows on the big map.
    //
    // Networking: this object has its own PhotonView with a fixed ID, so every player's game
    // knows it as "the same object". photonView.RPC(...) calls ReceivePing on every player's
    // copy of it, including your own. (Raid Mode uses the same trick, with ID 980.)
    class Pings : Photon.MonoBehaviour
    {
        const int ViewId = 981;
        const float Cooldown = 0.5f;
        const float MaxDistance = 200f;
        const float PulseSeconds = 0.35f;
        const float FadeSeconds = 2f;
        const float EdgeMargin = 48f;

        class Ping
        {
            public string Name;
            public Vector3 Position;
            public Color Color;
            public float CreatedAt;
            public float ExpiresAt;
            public GameObject MapAnchor;
        }

        // One ping per player: a new ping replaces your old one.
        readonly Dictionary<int, Ping> active = new Dictionary<int, Ping>();
        readonly List<int> expired = new List<int>();
        float nextPingTime;

        Texture2D diamond;
        Font gameFont;
        GUIStyle nameStyle;
        GUIStyle distanceStyle;

        public static void Create()
        {
            var obj = new GameObject("ExpeditionPings");
            DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            obj.AddComponent<Pings>();
            PhotonView view = obj.AddComponent<PhotonView>();
            view.viewID = ViewId;
        }

        static Character LocalCharacter()
        {
            return CharacterManager.Instance != null ? CharacterManager.Instance.GetFirstLocalCharacter() : null;
        }

        // The camera that follows your character. Falls back to Unity's "main" camera.
        static Camera PlayerCamera(Character me)
        {
            if (me != null && me.CharacterCamera != null && me.CharacterCamera.CameraScript != null)
            {
                return me.CharacterCamera.CameraScript;
            }
            return Camera.main;
        }

        void Update()
        {
            if (!Settings.Pings.Value || !Input.GetKeyDown(Settings.PingKey.Value) || Time.time < nextPingTime)
            {
                return;
            }

            Character me = LocalCharacter();
            Camera camera = PlayerCamera(me);
            if (me == null || camera == null)
            {
                return;
            }

            nextPingTime = Time.time + Cooldown;
            Vector3 spot = FindSpot(camera, me);

            if (PhotonNetwork.inRoom && !PhotonNetwork.offlineMode)
            {
                photonView.RPC(nameof(ReceivePing), PhotonTargets.All, me.Name, spot);
            }
            else
            {
                Show(0, me.Name, spot);
            }
        }

        // Shoot a ray from the middle of the screen and use the first thing it hits that
        // isn't you. Looking at the sky? Then the ping goes on the ground in front of you.
        static Vector3 FindSpot(Camera camera, Character me)
        {
            Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxDistance, ~0, QueryTriggerInteraction.Ignore);

            Vector3 spot = me.transform.position + me.transform.forward * 10f;
            float closest = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                if (hit.distance < closest && !hit.collider.transform.IsChildOf(me.transform))
                {
                    closest = hit.distance;
                    spot = hit.point;
                }
            }
            return spot;
        }

        // Runs on every player's PC when anyone pings.
        [PunRPC]
        public void ReceivePing(string name, Vector3 position, PhotonMessageInfo info)
        {
            int playerId = info.sender != null ? info.sender.ID : 0;
            Show(playerId, name, position);

            Character me = LocalCharacter();
            bool fromSomeoneElse = info.sender != null && !info.sender.IsLocal;
            if (fromSomeoneElse && me != null && me.CharacterUI != null)
            {
                me.CharacterUI.ShowInfoNotification(name + " pinged a spot");
            }
        }

        void Show(int playerId, string name, Vector3 position)
        {
            Ping old;
            if (active.TryGetValue(playerId, out old))
            {
                Remove(old);
            }

            var ping = new Ping
            {
                Name = name,
                Position = position,
                Color = PartyColors.ForActor(playerId),
                CreatedAt = Time.time,
                ExpiresAt = Time.time + Settings.PingSeconds.Value,
            };

            // Also put the ping on the big map, using the same game marker as the players.
            if (Settings.MapMarkers.Value)
            {
                ping.MapAnchor = new GameObject("ExpeditionPingAnchor");
                ping.MapAnchor.transform.position = position;
                PartyMap.AttachMarker(ping.MapAnchor.transform, name + "'s ping", ping.Color).gameObject.SetActive(true);
            }

            active[playerId] = ping;
        }

        static void Remove(Ping ping)
        {
            if (ping.MapAnchor != null)
            {
                Destroy(ping.MapAnchor);
            }
        }

        // OnGUI is Unity's simple way to draw on screen; it runs every frame.
        void OnGUI()
        {
            if (active.Count == 0)
            {
                return;
            }

            expired.Clear();
            foreach (KeyValuePair<int, Ping> entry in active)
            {
                if (Time.time > entry.Value.ExpiresAt)
                {
                    expired.Add(entry.Key);
                }
            }
            foreach (int id in expired)
            {
                Remove(active[id]);
                active.Remove(id);
            }

            Character me = LocalCharacter();
            Camera camera = PlayerCamera(me);
            if (camera == null)
            {
                return;
            }

            SetUpStyle(me);
            Vector3 from = me != null ? me.transform.position : camera.transform.position;
            foreach (Ping ping in active.Values)
            {
                DrawPing(camera, from, ping);
            }
            GUI.color = Color.white;
        }

        void DrawPing(Camera camera, Vector3 from, Ping ping)
        {
            // Fade in the last seconds, and "pop" when the ping first appears.
            float alpha = Mathf.Clamp01((ping.ExpiresAt - Time.time) / FadeSeconds);
            float pop = 1f - Mathf.Clamp01((Time.time - ping.CreatedAt) / PulseSeconds);
            float size = 20f * (1f + 0.8f * pop);

            // Screen position of the spot. Behind the camera, or off screen? Then pin the
            // marker to the nearest screen edge so it still shows which way to look.
            Vector3 screen = camera.WorldToScreenPoint(ping.Position);
            bool behind = screen.z < 0f;
            var point = new Vector2(screen.x, Screen.height - screen.y);
            if (behind)
            {
                point = new Vector2(Screen.width - point.x, Screen.height - point.y);
            }
            bool offScreen = behind
                || point.x < EdgeMargin || point.x > Screen.width - EdgeMargin
                || point.y < EdgeMargin || point.y > Screen.height - EdgeMargin;
            if (offScreen)
            {
                point.x = Mathf.Clamp(point.x, EdgeMargin, Screen.width - EdgeMargin);
                point.y = Mathf.Clamp(point.y, EdgeMargin, Screen.height - EdgeMargin);
                if (behind)
                {
                    point.y = Screen.height - EdgeMargin;
                }
            }

            // The diamond sits above the spot, joined to it by a thin stem.
            var diamondCenter = new Vector2(point.x, point.y - (offScreen ? 0f : 22f));
            if (!offScreen)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.6f * alpha);
                GUI.DrawTexture(new Rect(point.x - 2f, diamondCenter.y, 4f, 22f), Texture2D.whiteTexture);
                GUI.color = new Color(ping.Color.r, ping.Color.g, ping.Color.b, alpha);
                GUI.DrawTexture(new Rect(point.x - 1f, diamondCenter.y, 2f, 22f), Texture2D.whiteTexture);
            }

            float outline = size + 6f;
            GUI.color = new Color(0.12f, 0.09f, 0.06f, 0.85f * alpha);
            GUI.DrawTexture(new Rect(diamondCenter.x - outline / 2f, diamondCenter.y - outline / 2f, outline, outline), diamond);
            GUI.color = new Color(ping.Color.r, ping.Color.g, ping.Color.b, alpha);
            GUI.DrawTexture(new Rect(diamondCenter.x - size / 2f, diamondCenter.y - size / 2f, size, size), diamond);

            // Name in parchment, distance underneath in a dimmer shade.
            int metres = Mathf.RoundToInt(Vector3.Distance(from, ping.Position));
            float top = diamondCenter.y - outline / 2f - 44f;
            DrawLabel(new Rect(point.x - 150f, top, 300f, 24f), ping.Name, nameStyle, PartyColors.Parchment, alpha);
            DrawLabel(new Rect(point.x - 150f, top + 22f, 300f, 20f), metres + " m", distanceStyle, PartyColors.ParchmentDim, alpha);
        }

        static void DrawLabel(Rect area, string text, GUIStyle style, Color color, float alpha)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.8f * alpha);
            GUI.Label(new Rect(area.x + 1.5f, area.y + 1.5f, area.width, area.height), text, style);
            GUI.color = new Color(color.r, color.g, color.b, alpha);
            GUI.Label(area, text, style);
        }

        void SetUpStyle(Character me)
        {
            if (diamond == null)
            {
                diamond = MakeDiamond(64);
            }

            // Borrow the font Outward's own UI uses, from any text in the player's HUD.
            if (gameFont == null && me != null && me.CharacterUI != null)
            {
                foreach (UnityEngine.UI.Text text in me.CharacterUI.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    if (text.font != null)
                    {
                        gameFont = text.font;
                        nameStyle = null;
                        break;
                    }
                }
            }

            if (nameStyle == null)
            {
                nameStyle = new GUIStyle(GUI.skin.label)
                {
                    font = gameFont,
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.LowerCenter,
                };
                nameStyle.normal.textColor = Color.white;
                distanceStyle = new GUIStyle(nameStyle) { fontSize = 14, fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperCenter };
            }
        }

        // A white diamond with soft edges. GUI.color tints it to any colour when drawn.
        static Texture2D MakeDiamond(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = (Mathf.Abs(x - half) + Mathf.Abs(y - half)) / half;
                    float a = Mathf.Clamp01((1f - distance) * half / 1.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
