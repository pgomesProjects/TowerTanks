using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public class TimingGauge : SerializedMonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("The speed of the tick bar (how many seconds it takes for the tick bar to reach the other side.")] protected float tickSpeed;
    [SerializeField, Tooltip("The range of the target zone.")] protected Vector2 targetZoneRange;
    [Space(20)]
    [Header("Graphics")]
    [SerializeField, Tooltip("The tick bar transform that moves.")] protected RectTransform tickBar;
    [SerializeField, Tooltip("The target bar.")] protected RectTransform targetBar;
    [SerializeField, Tooltip("The target zone bar.")] protected RectTransform zoneBar;
    [Space]
    [Header("Animation Settings")]
    [SerializeField, Tooltip("The pulse amount of the tick bar.")] protected float tickBarPulse;
    [SerializeField, Tooltip("The duration of the animation.")] protected float hitAnimationDuration;
    [SerializeField, Tooltip("The ease type of the animation.")] protected LeanTweenType hitAnimationEaseType;
    [SerializeField, Tooltip("The color to flash when the zone is hit.")] protected Color hitZoneColor;
    [SerializeField, Tooltip("The color to flash when the zone is not hit.")] protected Color noHitZoneColor;

    protected bool timingActive = false;
    protected float tickMovementSpeed;
    protected float direction;
    protected Image zoneBarImage;
    protected Image tickBarImage;
    protected Color defaultColor;

    private float targetBarWidth;
    private Vector2 tickBarPos;

    [Button(ButtonSizes.Medium)]
    protected void DebugPressGauge()
    {
        Debug.Log("Hit Gauge: " + PressGauge());
    }

    protected virtual void Awake()
    {
        targetBarWidth = targetBar.rect.width;
        zoneBarImage = zoneBar.GetComponent<Image>();
        tickBarImage = tickBar.GetComponentInChildren<Image>();
        defaultColor = tickBarImage.color;
        timingActive = false;
        UpdateTargetZoneRange(targetZoneRange);
    }

    protected virtual void OnDisable()
    {
        timingActive = false;
    }

    public void CreateTimer(float tickSpeed, float minimumRange, float maximumRange)
    {
        this.tickSpeed = tickSpeed;
        targetZoneRange.x = minimumRange;
        targetZoneRange.y = maximumRange;
        UpdateTargetZoneRange(targetZoneRange);
        ActivateTimer();
    }

    public void ChangeColor(Color hitZoneColor, Color noHitZoneColor)
    {
        zoneBar.GetComponentInChildren<Image>().color = hitZoneColor;
        targetBar.GetComponent<Image>().color = noHitZoneColor;
    }

    /// <summary>
    /// Activates the timer gauge so that it begins to move.
    /// </summary>
    protected virtual void ActivateTimer()
    {
        if (Application.isPlaying)
        {
            tickBar.anchoredPosition = new Vector2(-targetBarWidth / 2, tickBar.anchoredPosition.y);
            tickBarPos = tickBar.anchoredPosition;
        }
        direction = 1;
        tickMovementSpeed = targetBarWidth / tickSpeed;
        timingActive = true;
    }

    /// <summary>
    /// Updates that target zone range.
    /// </summary>
    /// <param name="zoneRange">The minimum and maximum values of the zone range.</param>
    public virtual void UpdateTargetZoneRange(Vector2 zoneRange)
    {
        //Get the desired width of the zone
        float range = zoneRange.y - zoneRange.x;
        float desiredWidth = targetBarWidth * range;

        //Offset the width with the minimum range's offset
        float leftOffset = targetBarWidth * zoneRange.x;

        //Update the width of the zone bar mask
        zoneBar.offsetMin = new Vector2(leftOffset, zoneBar.offsetMin.y);
        zoneBar.offsetMax = new Vector2(leftOffset + desiredWidth - targetBarWidth, zoneBar.offsetMax.y);
    }

    /// <summary>
    /// Gets the value of the tick bar within the target.
    /// </summary>
    /// <returns>Returns a value from 0 to 1 based on the based on where it is on the target bar.</returns>
    protected virtual float GetTickBarPosition()
    {
        //Ensure that the tick bar stays within the bounds of the zone bar
        tickBar.anchoredPosition = new Vector2(Mathf.Clamp(tickBar.anchoredPosition.x, -targetBarWidth / 2, targetBarWidth / 2), tickBar.anchoredPosition.y);
        tickBarPos = tickBar.anchoredPosition;
        return Mathf.InverseLerp(-targetBarWidth / 2, targetBarWidth / 2, tickBar.anchoredPosition.x);
    }

    /// <summary>
    /// Moves the tick bar on the target bar.
    /// </summary>
    protected virtual void MoveTickBar()
    {
        tickBarPos.x += tickMovementSpeed * direction * Time.deltaTime;

        if(direction == 1)
        {
            //If the tick bar reaches the right bound, start moving left
            if (tickBarPos.x >= targetBarWidth / 2)
                direction = -1;
        }
        else if(direction == -1)
        {
            //If the tick bar reaches the left bound, start moving right
            if (tickBarPos.x <= -targetBarWidth / 2)
                direction = 1;
        }

        //Set the position of the tick bar and clamp it so that it does not go out of bounds
        tickBar.anchoredPosition = new Vector2(Mathf.Clamp(tickBarPos.x, -targetBarWidth / 2, targetBarWidth / 2), tickBarPos.y);
    }

    /// <summary>
    /// Presses the timing gauge.
    /// </summary>
    /// <returns>Returns true if hit within the zone. Returns false if not hit.</returns>
    public bool PressGauge()
    {
        //If the timing gauge is off, return false by default
        if (!timingActive)
            return false;

        bool inZone = InTargetZone(GetTickBarPosition());
        TickAnimation(inZone ? hitZoneColor : noHitZoneColor);
        return inZone;
    }

    /// <summary>
    /// Ends the timing gauge and destroys it.
    /// </summary>
    public void EndTimingGauge()
    {
        timingActive = false;
        Destroy(gameObject);
    }

    /// <summary>
    /// Animates the tick bar so that it pulses and flashes a color.
    /// </summary>
    /// <param name="flashColor">The color for the tick bar to flash.</param>
    protected void TickAnimation(Color flashColor)
    {
        //Pulse the tick bar's scale
        LeanTween.scale(tickBarImage.gameObject, Vector3.one * tickBarPulse, hitAnimationDuration).setEase(hitAnimationEaseType).setOnComplete(() => tickBarImage.transform.localScale = Vector3.one);

        //Ease between the default color and the target color, and then do the reverse back to its original color
        LeanTween.value(tickBarImage.gameObject, tickBarImage.color, flashColor, hitAnimationDuration).setEase(hitAnimationEaseType)
            .setOnUpdate((Color val) => tickBarImage.color = val)
            .setOnComplete(() => LeanTween.value(tickBarImage.gameObject, tickBarImage.color, defaultColor, hitAnimationDuration).setEase(hitAnimationEaseType)
            .setOnUpdate((Color val) => tickBarImage.color = val));
    }

    protected virtual void Update()
    {
#if UNITY_EDITOR
        //If working in the editor, update the target zone visual
        if (Application.isEditor && !Application.isPlaying)
        {
            timingActive = false;
            targetBarWidth = targetBar.rect.width;
            UpdateTargetZoneRange(targetZoneRange);
        }
#endif
        //If the timer is active, move the tick bar
        if (timingActive)
            MoveTickBar();
    }

    protected bool InTargetZone(float position) => position >= targetZoneRange.x && position <= targetZoneRange.y;
}
