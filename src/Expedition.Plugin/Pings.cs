using System.Collections.Generic;
using UnityEngine;

namespace Expedition.Plugin
{
    // Press the ping key (middle mouse by default) to mark the spot in front of you.
    // Every player sees a marker there with your name and the distance, for a few seconds.
    //
    // Networking: this object has its own PhotonView with a fixed ID, so every player's game
    // knows it as "the same object". photonView.RPC(...) calls ReceivePing on every player's
    // copy of it, including your own. (Raid Mode uses the same trick, with ID 980.)
    class Pings : Photon.MonoBehaviour
    {
        const int ViewId = 981;
        const float Cooldown = 0.5f;
        const float MaxDistance = 200f;

        class Ping
        {
            public string Name;
            public Vector3 Position;
            public float ExpiresAt;
        }

        // One ping per player: a new ping replaces your old one.
        readonly Dictionary<int, Ping> active = new Dictionary<int, Ping>();
        readonly List<int> expired = new List<int>();
        float nextPingTime;
        GUIStyle textStyle;
        GUIStyle shadowStyle;

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
            active[playerId] = new Ping
            {
                Name = name,
                Position = position,
                ExpiresAt = Time.time + Settings.PingSeconds.Value,
            };
        }

        // OnGUI is Unity's simple way to draw text on screen; it runs every frame.
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
                active.Remove(id);
            }

            Character me = LocalCharacter();
            Camera camera = PlayerCamera(me);
            if (camera == null)
            {
                return;
            }

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.LowerCenter,
                };
                textStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
                shadowStyle = new GUIStyle(textStyle);
                shadowStyle.normal.textColor = Color.black;
            }

            Vector3 from = me != null ? me.transform.position : camera.transform.position;

            foreach (Ping ping in active.Values)
            {
                // Screen position of the spot; z <= 0 means it's behind the camera.
                Vector3 screen = camera.WorldToScreenPoint(ping.Position);
                if (screen.z <= 0f)
                {
                    continue;
                }

                int metres = Mathf.RoundToInt(Vector3.Distance(from, ping.Position));
                string text = ping.Name + " · " + metres + " m\n▼";

                // Unity's screen y starts at the bottom, GUI's at the top.
                var area = new Rect(screen.x - 150f, Screen.height - screen.y - 60f, 300f, 60f);
                var shadow = new Rect(area.x + 2f, area.y + 2f, area.width, area.height);
                GUI.Label(shadow, text, shadowStyle);
                GUI.Label(area, text, textStyle);
            }
        }
    }
}
