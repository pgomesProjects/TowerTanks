using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class VacuumController : TankInteractable
    {
        [Header("Vacuum Settings:")]
        public bool isSucking;
        public Transform suctionPoint;
        public Transform depositPoint;
        public float suctionPower;
        public LayerMask suctionMask;
        private BoxCollider2D[] suctionZones;
        private List<Rigidbody2D> objectsInVacuum = new List<Rigidbody2D>();

        private void Start()
        {
            base.Start();

            suctionZones = transform.Find("VacuumContainer/SuctionZone").GetComponentsInChildren<BoxCollider2D>();
            SuctionZone script = suctionZones[0].gameObject.AddComponent<SuctionZone>();
            script.vacuum = this;
            Debug.Log("Found " + suctionZones.Length + " zones.");
        }

        public override void Use(bool overrideConditions = false)
        {
            base.Use(overrideConditions);

            if (cooldown <= 0) ToggleSuction();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isSucking)
            {
                foreach (Rigidbody2D rb in objectsInVacuum)
                {
                    Vector2 force = (suctionPoint.position - rb.transform.position) * Time.fixedDeltaTime * (suctionPower * 100);
                    if (rb.gravityScale > 1.9f) force *= 2f;
                    rb.AddForce(force, ForceMode2D.Force);
                }

                Collider2D[] items = Physics2D.OverlapCircleAll(suctionPoint.position, 0.2f, suctionMask);
                if (items.Length > 0)
                {
                    foreach (Collider2D collider in items)
                    {
                        ISuckable item = collider.GetComponent<ISuckable>();
                        if (item != null)
                        {
                            Rigidbody2D rb = item.GetRigidbody2D();
                            rb.transform.position = depositPoint.position;
                        }
                    }
                }
            }
        }

        public void ToggleSuction()
        {
            switch (isSucking)
            {
                case true: isSucking = false;
                    break;
                case false: isSucking = true;
                    break;
            }
        }

        public class SuctionZone : MonoBehaviour
        {
            public VacuumController vacuum;

            private void OnTriggerEnter2D(Collider2D collider)
            {
                ISuckable item = collider.GetComponent<ISuckable>();
                if (item != null)
                {
                    Rigidbody2D rb = item.GetRigidbody2D();
                    if (!vacuum.objectsInVacuum.Contains(rb))
                    {
                        vacuum.objectsInVacuum.Add(rb);
                    }
                }
            }

            private void OnTriggerStay2D(Collider2D collider)
            {
                ISuckable item = collider.GetComponent<ISuckable>();
                if (item != null)
                {
                    Rigidbody2D rb = item.GetRigidbody2D();
                    if (!vacuum.objectsInVacuum.Contains(rb))
                    {
                        vacuum.objectsInVacuum.Add(rb);
                    }
                }
            }

            private void OnTriggerExit2D(Collider2D collider)
            {
                ISuckable item = collider.GetComponent<ISuckable>();
                if (item != null)
                {
                    Rigidbody2D rb = item.GetRigidbody2D();
                    if (vacuum.objectsInVacuum.Contains(rb))
                    {
                        vacuum.objectsInVacuum.Remove(rb);
                    }
                }
            }
        }
    }
}
