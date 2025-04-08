using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public abstract class GamepadSelectable : MonoBehaviour
{
    [SerializeField, Tooltip("If true, only certain players can select the object.")] protected bool isPlayerExclusive;
    protected List<int> currentSelectorPlayerIndexes = new List<int>();
    [SerializeField] protected int ownerPlayerIndex = -1;
    protected bool isSelected;

    public abstract void OnCursorEnter(PlayerInput playerInput);
    public abstract void OnCursorExit(PlayerInput playerInput);
    public abstract void OnSelectObject(PlayerInput playerInput);

    public void AssignValidPlayer(PlayerInput playerInput)
    {
        Debug.Log("Assigning " + gameObject.name + " to Player " + playerInput.playerIndex + 1);
        ownerPlayerIndex = playerInput.playerIndex;
    }
    public void AddPlayerSelecting(PlayerInput playerInput) => currentSelectorPlayerIndexes.Add(playerInput.playerIndex);
    public void RemovePlayerSelecting(PlayerInput playerInput) => currentSelectorPlayerIndexes.Remove(playerInput.playerIndex);
    protected bool IsValidPlayer(int currentPlayerIndex) => !isPlayerExclusive || currentPlayerIndex == ownerPlayerIndex;
    public int GetOwnerIndex() => ownerPlayerIndex;
}
