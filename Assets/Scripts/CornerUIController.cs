using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornerUIController : MonoBehaviour
{
    [SerializeField] private Transform[] cornerUIObjects;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void OnPlayerJoined(int playerIndex)
    {
        cornerUIObjects[playerIndex].gameObject.SetActive(false);
    }
}
