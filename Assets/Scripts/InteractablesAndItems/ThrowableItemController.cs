using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowableItemController : MonoBehaviour
{
    private PlayerTankController playerTank;
    // Start is called before the first frame update
    void Start()
    {
        playerTank = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>();
    }

    // Update is called once per frame
    void Update()
    {
        float itemRange = playerTank.transform.position.x + playerTank.tankBarrierRange;
        Vector3 itemPos = transform.position;
        itemPos.x = Mathf.Clamp(itemPos.x, -itemRange, itemRange);
        transform.position = itemPos;
    }
}
