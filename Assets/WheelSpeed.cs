using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelSpeed : MonoBehaviour
{
    private float speed = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        speed = (FindObjectOfType<PlayerTankController>().GetPlayerSpeed()) * 10f;
        transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self);
    }
}
