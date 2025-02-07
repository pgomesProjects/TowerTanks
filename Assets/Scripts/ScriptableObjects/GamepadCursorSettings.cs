using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class GamepadCursorSprite
    {
        public Sprite mainSprite;
        public Sprite outlineSprite;
    }

    public enum GamepadCursorState
    {
        DEFAULT,
        SELECT,
        GRAB,
        DISABLED
    }

    [CreateAssetMenu(fileName = "New Gamepad Cursor Settings", menuName = "ScriptableObjects/Gamepad Cursor Settings")]
    public class GamepadCursorSettings : ScriptableObject
    {
        public GamepadCursorSprite defaultSprite;
        public GamepadCursorSprite selectSprite;
        public GamepadCursorSprite grabSprite;
        public GamepadCursorSprite disabledSprite;
    }
}
