using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Potsu.Surface
{
    /// <summary>
    /// Suit les appuis souris et tactiles (multitouch sur iOS) et signale chaque relâchement
    /// avec la position de l'appui et sa durée. Marche avec le nouvel Input System ou l'ancien Input Manager.
    /// </summary>
    public sealed class PointerTracker
    {
        const int MouseId = -1;

        struct Press
        {
            public Vector2 Position;
            public float StartTime;
        }

        readonly Dictionary<int, Press> active = new Dictionary<int, Press>();

        /// <summary>Un appui commence (identifiant, position écran en pixels).</summary>
        public event Action<int, Vector2> Pressed;

        /// <summary>Un appui se termine (identifiant, position de l'appui en pixels, durée en secondes).</summary>
        public event Action<int, Vector2, float> Released;

        public int ActiveCount => active.Count;

        /// <summary>Durée de maintien actuelle d'un appui en cours, ou -1.</summary>
        public float HeldTime(int id, float now) => active.TryGetValue(id, out var p) ? now - p.StartTime : -1f;

        public IEnumerable<int> ActiveIds => active.Keys;

        public bool TryGetPosition(int id, out Vector2 position)
        {
            bool found = active.TryGetValue(id, out var p);
            position = p.Position;
            return found;
        }

        public void Poll(float now)
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame) Begin(MouseId, mouse.position.ReadValue(), now);
                if (mouse.leftButton.wasReleasedThisFrame) End(MouseId, now);
            }

            var screen = Touchscreen.current;
            if (screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    if (touch.press.wasPressedThisFrame) Begin(touch.touchId.ReadValue(), touch.position.ReadValue(), now);
                    if (touch.press.wasReleasedThisFrame) End(touch.touchId.ReadValue(), now);
                }
            }
#else
            Input.simulateMouseWithTouches = false;

            if (Input.GetMouseButtonDown(0)) Begin(MouseId, Input.mousePosition, now);
            if (Input.GetMouseButtonUp(0)) End(MouseId, now);

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        Begin(touch.fingerId, touch.position, now);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        End(touch.fingerId, now);
                        break;
                }
            }
#endif
        }

        void Begin(int id, Vector2 position, float now)
        {
            active[id] = new Press { Position = position, StartTime = now };
            Pressed?.Invoke(id, position);
        }

        void End(int id, float now)
        {
            if (!active.TryGetValue(id, out var press)) return;
            active.Remove(id);
            Released?.Invoke(id, press.Position, now - press.StartTime);
        }
    }
}
