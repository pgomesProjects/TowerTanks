using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts.Deprecated
{
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
            if (FindObjectOfType<PlayerTankController>() != null)
            {
                speed = (FindObjectOfType<PlayerTankController>().GetBaseTankSpeed()) * 20f;
                direction = (-FindObjectOfType<PlayerTankController>().GetThrottleMultiplier());
                if (direction != 0 && isEnemyTank == false)
                {
                    transform.Rotate(0f, 0f, speed * direction * Time.deltaTime, Space.Self);
                }

                if (isEnemyTank)
                {
                    transform.Rotate(0f, 0f, 40f * Time.deltaTime, Space.Self);
                }
            }
        }
    }

}