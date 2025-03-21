using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class TutorialManager : MonoBehaviour
    {
        public StructureController tutorialStructure;

        public int tutorialStep = 0;
        public List<Transform> locks = new List<Transform>();
        public LayerMask couplerMask;

        // Start is called before the first frame update
        void Start()
        {
            GetLocks();
            LockCouplers();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void GetLocks()
        {
            Transform zoneParent = tutorialStructure.transform.Find("Zones");
            if (zoneParent != null)
            {
                foreach(Transform child in zoneParent)
                {
                    if (child.name.Contains("Lock"))
                    {
                        locks.Add(child);
                    }
                }
                
            }
        }

        private void LockCouplers()
        {
            foreach(Transform _lock in locks)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(_lock.position, 1f, couplerMask);

                if (colliders.Length > 0)
                {
                    foreach(Collider2D collider in colliders)
                    {
                        Coupler coupler = collider.GetComponent<Coupler>();
                        if (coupler != null) coupler.LockCoupler();
                    }
                }

                _lock.gameObject.SetActive(false);
            }
        }
    }
}
