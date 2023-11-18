using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public abstract class MultiplayerUI : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI joinPrompt;
    private void OnEnable()
    {
        GameManager.Instance.MultiplayerManager.OnPlayerConnected += OnPlayerJoined;
        GameManager.Instance.MultiplayerManager.OnPlayerLost += OnPlayerLost;
        GameManager.Instance.MultiplayerManager.OnPlayerRegained += OnPlayerRejoined;
    }

    private void OnDisable()
    {
        GameManager.Instance.MultiplayerManager.OnPlayerConnected -= OnPlayerJoined;
        GameManager.Instance.MultiplayerManager.OnPlayerLost -= OnPlayerLost;
        GameManager.Instance.MultiplayerManager.OnPlayerRegained -= OnPlayerRejoined;
    }

    public abstract void OnPlayerJoined(PlayerInput playerInput);
    public abstract void OnPlayerLost(int playerIndex);
    public abstract void OnPlayerRejoined(int playerIndex);

    public virtual void ShowJoinPrompt(bool showPrompt) => joinPrompt.gameObject.SetActive(showPrompt);
    public virtual bool IsJoinPromptActive() => joinPrompt.gameObject.activeInHierarchy;
}
