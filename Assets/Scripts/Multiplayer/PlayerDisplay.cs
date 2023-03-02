using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDisplay : MonoBehaviour
{
    [SerializeField] private Image playerIcon;
    [SerializeField] private Image playerIconBackground;
    [SerializeField] private Image playerControlScheme;

    [SerializeField] private Sprite playerJoinedIcon;
    [SerializeField] private Sprite playerNotJoinedIcon;

    [SerializeField] private Sprite gamepadControlSchemeIcon;
    [SerializeField] private Sprite keyboardControlSchemeIcon;

    private Color defaultBackgroundColor;
    private Color currentPlayerColor;

    private void Start()
    {
        defaultBackgroundColor = playerIconBackground.color;
    }

    private void OnDisable()
    {
        //HidePlayerInfo();
    }

    public void ShowPlayerInfo()
    {
        playerIconBackground.color = currentPlayerColor;
        playerIcon.sprite = playerJoinedIcon;
        playerControlScheme.gameObject.SetActive(true);
    }

    public void ShowPlayerInfo(Color playerColor, string controlScheme)
    {
        currentPlayerColor = playerColor;
        playerIconBackground.color = currentPlayerColor;

        playerIcon.sprite = playerJoinedIcon;
        playerControlScheme.gameObject.SetActive(true);

        if (controlScheme == "Gamepad")
            playerControlScheme.sprite = gamepadControlSchemeIcon;
        else if (controlScheme == "Keyboard and Mouse")
            playerControlScheme.sprite = keyboardControlSchemeIcon;

        playerControlScheme.color = playerColor;
    }

    public void HidePlayerInfo()
    {
        playerIconBackground.color = defaultBackgroundColor;
        playerIcon.sprite = playerNotJoinedIcon;
        playerControlScheme.gameObject.SetActive(false);
    }
}
