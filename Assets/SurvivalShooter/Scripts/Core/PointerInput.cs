using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Thin wrapper over the Input System that treats mouse (Editor / XR Simulation) and touch
    /// (device) the same way, and filters out presses that land on UI.
    /// </summary>
    public static class PointerInput
    {
        private static readonly List<RaycastResult> UiHits = new List<RaycastResult>();
        private static PointerEventData eventData;
        private static EventSystem cachedSystem;

        public static bool PressedThisFrame(out Vector2 position)
        {
            position = default;
            Pointer p = Pointer.current;
            if (p == null) return false;
            position = p.position.ReadValue();
            return p.press.wasPressedThisFrame;
        }

        public static bool IsHeld(out Vector2 position)
        {
            position = default;
            Pointer p = Pointer.current;
            if (p == null) return false;
            position = p.position.ReadValue();
            return p.press.isPressed;
        }

        public static bool IsOverUI(Vector2 screenPosition)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            if (eventData == null || cachedSystem != es) { eventData = new PointerEventData(es); cachedSystem = es; }
            eventData.position = screenPosition;
            UiHits.Clear();
            es.RaycastAll(eventData, UiHits);
            return UiHits.Count > 0;
        }

        /// <summary>A press this frame that did NOT hit any UI element.</summary>
        public static bool WorldTapThisFrame(out Vector2 position)
        {
            return PressedThisFrame(out position) && !IsOverUI(position);
        }
    }
}
