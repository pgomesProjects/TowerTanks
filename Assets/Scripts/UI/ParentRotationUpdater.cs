using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class ParentRotationUpdater : MonoBehaviour
    {
        private Transform parentTransform;

        private void OnEnable()
        {
            parentTransform = transform.parent;
        }

        private void Update()
        {
            //If any components are missing, ignore
            if (parentTransform == null)
                return;

            transform.localEulerAngles = new Vector3(parentTransform.eulerAngles.x, parentTransform.eulerAngles.y, parentTransform.eulerAngles.z);
        }
    }
}