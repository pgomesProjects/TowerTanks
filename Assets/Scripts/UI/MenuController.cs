using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem.UI;

public class MenuController : MonoBehaviour
{
    [SerializeField] private string[] menuOptions;
    private Color normalColor;
    [SerializeField] private Color disabledColor;
    [SerializeField] private Color highlightColor;
    public MenuEvent OnValueChanged;
    private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI controlText;
    private Image leftArrow;
    private Image rightArrow;
    private int controlIndex = 0;
    private bool isSelected;

    private Vector2 menuMovement;
    private float moveRepeatDelay;
    private float moveRepeatRate;

    private IEnumerator currentDirectionCoroutine;
    private bool leftMoveActive, rightMoveActive;

    private PlayerControlSystem playerControlSystem;

    private void Awake()
    {
        playerControlSystem = new PlayerControlSystem();
    }

    private void OnEnable()
    {
        playerControlSystem.Enable();
        UpdateValue();

        leftMoveActive = false;
        rightMoveActive = false;
        moveRepeatDelay = ((InputSystemUIInputModule)EventSystem.current.currentInputModule).moveRepeatDelay;
        moveRepeatRate = ((InputSystemUIInputModule)EventSystem.current.currentInputModule).moveRepeatRate;

        labelText = transform.Find("Label").GetComponent<TextMeshProUGUI>();
        leftArrow = transform.Find("PrevArrow").GetComponent<Image>();
        rightArrow = transform.Find("NextArrow").GetComponent<Image>();
        normalColor = controlText.color;

        isSelected = false;
    }

    private void OnDisable()
    {
        playerControlSystem.Disable();
        OnDeselect();
    }

    private void Update()
    {
        if (isSelected)
        {
            menuMovement = playerControlSystem.UI.Navigate.ReadValue<Vector2>();

            //Moving left
            if(menuMovement.x < -0.1f)
            {
                if(currentDirectionCoroutine != null)
                {
                    if(!leftMoveActive)
                    {
                        StopCoroutine(currentDirectionCoroutine);
                        rightMoveActive = false;
                        currentDirectionCoroutine = MoveControlLeft();
                        StartCoroutine(currentDirectionCoroutine);
                    }
                }
                else
                {
                    currentDirectionCoroutine = MoveControlLeft();
                    StartCoroutine(currentDirectionCoroutine);
                }
            }
            //Moving right
            else if(menuMovement.x > 0.1f)
            {
                if (currentDirectionCoroutine != null)
                {
                    if (!rightMoveActive)
                    {
                        StopCoroutine(currentDirectionCoroutine);
                        leftMoveActive = false;
                        currentDirectionCoroutine = MoveControlRight();
                        StartCoroutine(currentDirectionCoroutine);
                    }
                }
                else
                {
                    currentDirectionCoroutine = MoveControlRight();
                    StartCoroutine(currentDirectionCoroutine);
                }
            }
        }
    }

    private IEnumerator MoveControlLeft()
    {
        Debug.Log("Initial Move Left");
        NavigateLeft();
        leftMoveActive = true;

        float timer = 0;
        while (timer < moveRepeatDelay && isSelected && menuMovement.x < -0.1f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        while (isSelected && menuMovement.x < -0.1f)
        {
            Debug.Log("Repeat Move Left");
            yield return new WaitForSeconds(moveRepeatRate);
            NavigateLeft();
        }

        leftMoveActive = false;
    }

    private IEnumerator MoveControlRight()
    {
        Debug.Log("Initial Move Right");
        NavigateRight();
        rightMoveActive = true;

        float timer = 0;
        while(timer < moveRepeatDelay && isSelected && menuMovement.x > 0.1f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        while (isSelected && menuMovement.x > 0.1f)
        {
            Debug.Log("Repeat Move Right");
            yield return new WaitForSeconds(moveRepeatRate);
            NavigateRight();
        }
        rightMoveActive = false;
    }

    private void NavigateLeft()
    {
        if (controlIndex > 0)
        {
            controlIndex--;
            UpdateValue();
            OnValueChanged.Invoke(controlIndex);
        }
    }

    private void NavigateRight()
    {
        Debug.Log("Control Index: " + controlIndex);
        if (controlIndex < menuOptions.Length - 1)
        {
            controlIndex++;
            UpdateValue();
            OnValueChanged.Invoke(controlIndex);
        }
    }

    private void UpdateValue()
    {
        controlText.text = menuOptions[controlIndex];

        if (isSelected)
            CheckDisabledArrows();
    }

    public void CheckDisabledArrows()
    {
        if (controlIndex <= 0)
        {
            leftArrow.color = disabledColor;
        }
        else
            leftArrow.color = highlightColor;

        if (controlIndex >= menuOptions.Length - 1)
        {
            rightArrow.color = disabledColor;
        }
        else
            rightArrow.color = highlightColor;
    }

    public void OnSelect()
    {
        labelText.color = highlightColor;
        controlText.color = highlightColor;
        leftArrow.color = highlightColor;
        rightArrow.color = highlightColor;
        CheckDisabledArrows();
        isSelected = true;
    }

    public void OnDeselect()
    {
        labelText.color = normalColor;
        controlText.color = normalColor;
        leftArrow.color = normalColor;
        rightArrow.color = normalColor;

        isSelected = false;
    }

    public int GetIndex() => controlIndex;

    public void SetIndex(int index)
    {
        controlIndex = index;
        UpdateValue();
    }
}
