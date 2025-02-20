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

            //If the rotation is not equal to the parent transform, update it
            if(!Equals(transform.localRotation, parentTransform.rotation))
                transform.localRotation = parentTransform.rotation;
        }
    }
}
