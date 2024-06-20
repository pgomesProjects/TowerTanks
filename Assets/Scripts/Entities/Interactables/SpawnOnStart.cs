using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SpawnOnStart : MonoBehaviour
{
    public UnityEvent spawnEvent;

    // Start is called before the first frame update
    void Start()
    {
        spawnEvent.Invoke();
    }
}
