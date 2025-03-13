using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class GyroscopeComponent : MonoBehaviour
    {
        private void LateUpdate()
        {
            //Sets the global rotation of the item to identity (zero)
            transform.rotation = Quaternion.identity;
        }
    }
}