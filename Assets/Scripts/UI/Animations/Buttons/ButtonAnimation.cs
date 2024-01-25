using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class ButtonAnimation : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Vector2 startingPosition;
    [SerializeField] private Vector2 endingPosition;
    [SerializeField] private float onSelectDuration;
    [SerializeField] private float onDeselectDuration;
    [SerializeField] private AnimationCurve onSelectCurve;
    [SerializeField] private AnimationCurve onDeselectCurve;

    private RectTransform buttonTransform;

    private void Awake()
    {
        buttonTransform = GetComponent<RectTransform>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        LeanTween.move(buttonTransform, endingPosition, onSelectDuration).setEase(onSelectCurve);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        LeanTween.move(buttonTransform, startingPosition, onDeselectDuration).setEase(onDeselectCurve);
    }
}
