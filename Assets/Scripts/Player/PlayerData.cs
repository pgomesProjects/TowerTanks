using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerData : MonoBehaviour
{
    public PlayerInput playerInput { get; private set; }
    public InputActionMap playerInputMap { get; private set; }
    public Vector2 movementData { get; private set; }
    internal bool isBuilding;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerInputMap = playerInput.actions.FindActionMap("Player");
    }

    private void OnEnable()
    {
        playerInputMap.actionTriggered += OnPlayerInput;
    }

    private void OnDisable()
    {
        playerInputMap.actionTriggered -= OnPlayerInput;
    }

    private void OnPlayerInput(InputAction.CallbackContext ctx)
    {
        //Gets the name of the action and calls the appropriate events
        switch (ctx.action.name)
        {
            case "Move": OnMove(ctx); break;
            case "Rotate": OnRotate(ctx); break;
            case "Mount": OnMount(ctx); break;
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        movementData = ctx.ReadValue<Vector2>();
/*        float moveSensitivity = 0.2f;

        if (isBuilding)
        {
            if (ctx.started && movementData.y > moveSensitivity)
                BuildingManager.Instance.SnapRoom(playerInput, BuildingManager.WorldRoomSnap.UP);

            if (ctx.started && movementData.y < -moveSensitivity)
                BuildingManager.Instance.SnapRoom(playerInput, BuildingManager.WorldRoomSnap.DOWN);

            if (ctx.started && movementData.x > moveSensitivity)
                BuildingManager.Instance.SnapRoom(playerInput, BuildingManager.WorldRoomSnap.RIGHT);

            if (ctx.started && movementData.x < -moveSensitivity)
                BuildingManager.Instance.SnapRoom(playerInput, BuildingManager.WorldRoomSnap.LEFT);
        }*/
    }

    private void OnRotate(InputAction.CallbackContext ctx)
    {
        if (isBuilding && ctx.started)
        {
            BuildingManager.Instance.RotateRoom(playerInput);
        }
    }

    private void OnMount(InputAction.CallbackContext ctx)
    {
        if (isBuilding && ctx.started)
        {
            isBuilding = !BuildingManager.Instance.MountRoom(playerInput);
        }
    }
}
