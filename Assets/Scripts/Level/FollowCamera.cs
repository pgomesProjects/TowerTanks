using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private bool followX;
    [SerializeField] private bool followY;
    [SerializeField] private bool followZ;

    private Vector3 initialOffset;

    // Start is called before the first frame update
    void Start()
    {
        initialOffset = Camera.main.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (followX)
        {
            Vector3 newPos = Camera.main.transform.position - initialOffset;
            newPos = new Vector3(newPos.x, 0, 0);
            transform.position = newPos;
        }

        if (followY)
        {
            Vector3 newPos = Camera.main.transform.position - initialOffset;
            newPos = new Vector3(0, newPos.y, 0);
            transform.position = newPos;
        }

        if (followZ)
        {
            Vector3 newPos = Camera.main.transform.position - initialOffset;
            newPos = new Vector3(0, 0, newPos.z);
            transform.position = newPos;
        }
    }
}
