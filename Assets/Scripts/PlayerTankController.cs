using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
    [SerializeField] private float speed = 4;
    [SerializeField] internal float tankBarrierRange = 12;

    public float GetPlayerSpeed()
    {
        return speed;
    }
}
