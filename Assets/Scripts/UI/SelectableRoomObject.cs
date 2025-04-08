using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using UnityEngine.Events;
using TMPro;

namespace TowerTanks.Scripts
{
    public class SelectableRoomObject : GamepadSelectable
    {
        [SerializeField] private Image selectableImage;
        [SerializeField] private Color hoverColor;
        [SerializeField] private Color selectColor;
        [Space]
        [SerializeField] private Image roomSprite;
        [SerializeField] private TextMeshProUGUI roomAltText;

        private Color defaultColor;
        private int roomID;
        public bool roomHasHatch;

        internal UnityEvent<PlayerInput, int, bool> OnSelected = new UnityEvent<PlayerInput, int, bool>();

        private void Awake()
        {
            defaultColor = selectableImage.color;
            isSelected = false;
        }

        private int hoveringCursorCount = 0;

        public override void OnCursorEnter(PlayerInput playerInput)
        {
            if (!isSelected)
            {
                // Cursor entered the object, increase the count
                hoveringCursorCount++;
                selectableImage.color = hoverColor;
            }
        }

        public override void OnCursorExit(PlayerInput playerInput)
        {
            if (!isSelected)
            {
                // Cursor exited the object, decrease the count
                hoveringCursorCount--;

                // Check if no cursors are hovering, then return to default state
                if (hoveringCursorCount <= 0)
                {
                    hoveringCursorCount = 0;
                    selectableImage.color = defaultColor;
                }
            }
        }

        public void SetRoomID(int newID)
        {
            roomID = newID;
        }

        public override void OnSelectObject(PlayerInput playerInput)
        {
            if (!isSelected)
            {
                selectableImage.color = selectColor;
                isSelected = true;
                OnSelected?.Invoke(playerInput, roomID, roomHasHatch);
            }
        }

        public void DeselectRoom()
        {
            selectableImage.color = defaultColor;
            isSelected = false;
        }

        public void DisplayRoomInfo(RoomInfo roomInfo)
        {
            if (roomInfo.sprite == null)
            {
                roomAltText.text = roomInfo.name;
                roomSprite.color = new Color(0, 0, 0, 0);
            }
            else
            {
                roomAltText.text = "";
                roomSprite.sprite = roomInfo.sprite;
            }
        }
    }
}
