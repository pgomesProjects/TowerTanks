using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public abstract class MultiplayerUI : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI joinPrompt;

    public abstract void OnPlayerJoined(int playerIndex, Color playerColor, string controlScheme);
    public abstract void OnPlayerLost(int playerIndex);
    public abstract void OnPlayerRejoined(int playerIndex);

    public virtual void ShowJoinPrompt(bool showPrompt) => joinPrompt.gameObject.SetActive(showPrompt);
    public virtual bool IsJoinPromptActive() => joinPrompt.gameObject.activeInHierarchy;
}
