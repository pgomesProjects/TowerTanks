using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class TutorialManager : MonoBehaviour
    {
        public StructureController tutorialStructure;

        [InlineButton("NextTutorialStep", "Next")]
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
                        if (coupler != null)
                        {
                            coupler.LockCoupler();
                            break;
                        }
                    }
                }

                _lock.gameObject.SetActive(false);
            }

            locks[0].gameObject.SetActive(true);
        }

        private void UnlockCoupler(int index)
        {
            locks[index].gameObject.SetActive(false);
            locks[index + 1].gameObject.SetActive(true);
            Transform target = locks[index];

            Collider2D[] colliders = Physics2D.OverlapCircleAll(target.position, 1f, couplerMask);

            if (colliders.Length > 0)
            {
                foreach (Collider2D collider in colliders)
                {
                    Coupler coupler = collider.GetComponent<Coupler>();
                    if (coupler == null) coupler = collider.GetComponentInParent<Coupler>();
                    if (coupler != null)
                    {
                        if (coupler.locked) coupler.UnlockCoupler();
                    }
                }
            }
        }

        public void NextTutorialStep()
        {
            UnlockCoupler(tutorialStep);
            tutorialStep += 1;
        }
    }
}
