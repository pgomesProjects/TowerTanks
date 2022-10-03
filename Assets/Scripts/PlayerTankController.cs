using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
    [SerializeField] private float speed = 4;

    public float GetPlayerSpeed()
    {
        return speed;
    }
}
