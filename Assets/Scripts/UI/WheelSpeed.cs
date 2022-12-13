using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelSpeed : MonoBehaviour
{
    [SerializeField] private bool isEnemyTank = false;
    private float speed = 0;
    private float direction = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        speed = (FindObjectOfType<PlayerTankController>().GetPlayerSpeed()) * 20f;
        direction = (FindObjectOfType<LevelManager>().GetGameSpeed());
        if (direction != 0)
        {
            transform.Rotate(0f, 0f, speed * direction * Time.deltaTime, Space.Self);
        }
    }
}
