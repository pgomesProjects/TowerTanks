using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomBuildingMenu : MonoBehaviour
{
    [SerializeField, Tooltip("The prefab that generates data for a room that the players can pick.")] private GameObject roomIconPrefab;
    [SerializeField, Tooltip("The transform to store the selectable rooms.")] private Transform roomListTransform;
    [SerializeField, Tooltip("The RectTransform for the building menu.")] private RectTransform buildingMenuRectTransform;
    [SerializeField, Tooltip("The ending position for the menu.")] private Vector3 endingPos;
    [SerializeField, Tooltip("The duration for the menu animation.")] private float menuAniDuration = 0.5f;
    [SerializeField, Tooltip("The ease type for the animation.")] private LeanTweenType openMenuEaseType;
    [SerializeField, Tooltip("The ease type for the animation.")] private LeanTweenType closeMenuEaseType;

    private Vector3 startingMenuPos;

    // Start is called before the first frame update
    void Start()
    {
        startingMenuPos = buildingMenuRectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        GamePhaseUI.OnBuildingPhase += OpenMenu;
        GamePhaseUI.OnCombatPhase += CloseMenu;
    }

    private void OnDisable()
    {
        GamePhaseUI.OnBuildingPhase -= OpenMenu;
        GamePhaseUI.OnCombatPhase -= CloseMenu;
    }

    public void OpenMenu()
    {
        GenerateRooms();
        LeanTween.move(buildingMenuRectTransform, endingPos, menuAniDuration).setEase(openMenuEaseType);
    }

    public void CloseMenu()
    {
        LeanTween.move(buildingMenuRectTransform, startingMenuPos, menuAniDuration).setEase(closeMenuEaseType);
    }

    /// <summary>
    /// Generates a collection of rooms for the players to pick.
    /// </summary>
    public void GenerateRooms()
    {
        //Clear any existing rooms
        foreach(Transform rooms in roomListTransform)
            Destroy(rooms.gameObject);

        //Spawn in four random rooms and display their names
        for(int i = 0; i < 4; i++)
        {
            int roomIndexRange = Random.Range(0, LevelManager.Instance.roomList.Length);
            GameObject newRoom = Instantiate(roomIconPrefab, roomListTransform);
            newRoom.GetComponentInChildren<TextMeshProUGUI>().text = LevelManager.Instance.roomList[roomIndexRange].name.ToString();
        }
    }
}
