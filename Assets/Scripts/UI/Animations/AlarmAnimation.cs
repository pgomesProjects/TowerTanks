using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlarmAnimation : MonoBehaviour
{
    [Header("Warning Label Settings")]
    [SerializeField, Tooltip("The Warning Label component.")] private GameObject warningLabel;
    [SerializeField, Tooltip("The ease type for the warning label animation.")] private AnimationCurve startingAnimationCurve;
    [SerializeField, Tooltip("The ease type for the warning label animation.")] private AnimationCurve endAnimationCurve;
    [SerializeField, Tooltip("The number of seconds for the warning label animation to take.")] private float warningLabelDuration;
    [Space(10)]

    [Header("Alarm Overlay Settings")]
    [SerializeField, Tooltip("The Overlay RectTransform component.")] private RectTransform overlayRectTransform;
    [SerializeField, Range(0,1), Tooltip("The ending alpha for the warning overlay.")] private float endAlpha;
    [SerializeField, Tooltip("The ease type for the overlay flash.")] private LeanTweenType overlayEaseType;
    [SerializeField, Tooltip("The number of seconds for the flashing animation to take.")] private float flashingDuration;
    [SerializeField, Tooltip("The number of flashes for the alarm to show.")] private int numberOfFlashes;

    private void OnEnable()
    {
        //Reset values
        warningLabel.transform.localScale = Vector3.zero;
        Image overlayImage = overlayRectTransform.GetComponent<Image>();
        overlayImage.color = new Color(overlayImage.color.r, overlayImage.color.g, overlayImage.color.b, 0f);

        PlayAlarmAnimation();
    }

    /// <summary>
    /// Plays the alarm animation with sound.
    /// </summary>
    private void PlayAlarmAnimation()
    {
        FindObjectOfType<AudioManager>().Play("EnemyAlarm", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        //Warning label animation
        LeanTween.scale(warningLabel, Vector3.one, warningLabelDuration).setEase(startingAnimationCurve);

        //Alarm flash animation
        LeanTween.alpha(overlayRectTransform, endAlpha, flashingDuration).setEase(overlayEaseType).setLoopPingPong(numberOfFlashes).setOnComplete(EndAnimation);
    }

    /// <summary>
    /// Plays an ending animation when the animation is complete.
    /// </summary>
    private void EndAnimation()
    {
        LeanTween.scale(warningLabel, Vector3.zero, warningLabelDuration).setEase(endAnimationCurve).setOnComplete(() => gameObject.SetActive(false));
    }
}
