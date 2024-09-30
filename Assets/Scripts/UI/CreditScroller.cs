using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditScroller : MonoBehaviour
{
    [SerializeField, Tooltip("The RectTransform to scroll.")] private RectTransform creditRectTransform;
    [SerializeField, Tooltip("The speed of the credit scroll.")] private float creditScrollSpeed = 5f;
    [SerializeField, Tooltip("The multiplier for fast forwarding the credit scroll.")] private float fastForwardMultiplier = 2f;
    [SerializeField, Tooltip("The point where scrolling slows down near the end of the credits.")] private float slowDownThreshold = 100f;
    [SerializeField, Tooltip("The starting position for the credits.")] private float startCreditsYPos;
    [SerializeField, Tooltip("The ending position for the credits.")] private float endCreditsYPos;

    private float currentScrollSpeed;
    private float setScrollSpeed;
    private float slowDownMultiplier;
    private bool creditsRolling;
    private PlayerControlSystem playerControls;

    private void Awake()
    {
        playerControls = new PlayerControlSystem();
        playerControls.UI.FastForwardCredits.performed += _ => ToggleFastForward(true);
        playerControls.UI.FastForwardCredits.canceled += _ => ToggleFastForward(false);
    }

    private void OnEnable()
    {
        creditRectTransform.anchoredPosition = new Vector2(creditRectTransform.anchoredPosition.x, startCreditsYPos);
        setScrollSpeed = creditScrollSpeed;
        slowDownMultiplier = 1f;
        creditsRolling = true;
        GameManager.Instance.MultiplayerManager.EnablePlayersJoin(false);
        playerControls?.Enable();
    }

    private void OnDisable()
    {
        GameManager.Instance.MultiplayerManager.EnablePlayersJoin(true);
        playerControls?.Disable();
    }

    private void Update()
    {
        if (!creditsRolling) return;

        ScrollCredits();
    }

    private void ToggleFastForward(bool isFastForward)
    {
        setScrollSpeed = isFastForward ? creditScrollSpeed * fastForwardMultiplier : creditScrollSpeed;
    }

    private void ScrollCredits()
    {
        Vector2 currentPos = creditRectTransform.anchoredPosition;

        float distanceToEnd = Mathf.Abs(endCreditsYPos - currentPos.y);

        if (distanceToEnd <= slowDownThreshold)
            slowDownMultiplier = Mathf.Clamp01(distanceToEnd / slowDownThreshold);

        currentScrollSpeed = setScrollSpeed * slowDownMultiplier;

        // Move the credits upward by modifying the anchored position
        currentPos.y += currentScrollSpeed * Time.deltaTime;
        creditRectTransform.anchoredPosition = currentPos;

        // Stop the credits when they reach the end position
        if (currentPos.y >= endCreditsYPos)
            creditsRolling = false;
    }
}
