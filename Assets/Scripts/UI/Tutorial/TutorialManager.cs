using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    public class TutorialManager : MonoBehaviour
    {
        public StructureController tutorialStructure;
        public List<Transform> locks = new List<Transform>();
        public Animator[] panelAnimators;
        public LayerMask couplerMask;

        [Header("Tutorial Step Variables:")]
        [InlineButton("NextTutorialStep", "Next")]
        public int tutorialStep = 0;

        [Header("STEP 0: GATHER")]
        public InteractableZone zone_0;
        [Header("STEP 1: GATHER")]
        public Dispenser dispenser_1;
        public InteractableZone zone_1;
        [Header("STEP 2: THROTTLES")]
        public ThrottleController[] throttles_2;
        [Header("STEP 3: ENGINES")]
        public EngineController[] engines_3;
        [Header("STEP 4: WEAPONS")]
        public GameObject[] walls_4;
        [Header("STEP 5: ITEMS")]
        public Dispenser dispenser_5;
        public HopperHitbox hopper_5;
        [Header("STEP 6: TOOLS")]
        public Dispenser[] dispensers_6;
        public ThrottleController throttle_6;
        [Header("STEP 7: UNINSTALL")]
        public Dispenser dispenser_7;
        public ThrottleController throttle_7;
        //tank core_7

        private void Awake()
        {
            panelAnimators = transform.parent.Find("Zones/Hidden").GetComponentsInChildren<Animator>();
            foreach(Animator animator in panelAnimators)
            {
                animator.enabled = true;
                animator.transform.GetComponentInChildren<SpriteRenderer>().enabled = true;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            GetLocks();
            LockCouplers();
        }

        // Update is called once per frame
        void Update()
        {
            CheckTutorialState();
        }

        private void CheckTutorialState()
        {
            switch (tutorialStep)
            {
                case 0: //Gather
                    if (zone_0.playerIsColliding) NextTutorialStep();
                    break;

                case 1: //Gather
                    if (zone_1.playerIsColliding) NextTutorialStep();
                    break;

                case 2: //Throttles
                    bool ready_2 = true;
                    foreach (ThrottleController throttle in throttles_2)
                    {
                        if (throttle.gear != 2) ready_2 = false;
                    }
                    if (ready_2) NextTutorialStep();
                    break;

                case 3: //Engines
                    bool ready_3 = true;
                    foreach (EngineController engine in engines_3)
                    {
                        if (!engine.isPowered) ready_3 = false;
                    }
                    if (ready_3) NextTutorialStep();
                    break;

                case 4: //Weapons
                    bool ready_4 = true;
                    foreach (GameObject wall in walls_4)
                    {
                        if (wall != null) ready_4 = false;
                    }
                    if (ready_4) NextTutorialStep();
                    break;

                case 5: //Items
                    if (hopper_5.itemsSold >= 4) NextTutorialStep();
                    break;

                case 6: //Tools & Fire
                    bool extinguished_6 = true;
                    foreach (Cell cell in throttle_6.parentCell.room.cells)
                    {
                        if (cell.isOnFire) extinguished_6 = false;
                    }
                    if (extinguished_6 && dispensers_6[1].enabled != true) dispensers_6[1].enabled = true;
                    if (throttle_6.gear == 2) NextTutorialStep();
                    break;

                case 7: //Uninstall
                    if (throttle_7 == null) NextTutorialStep();
                    break;

                case 8: //Garage
                    break;
            }
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
            if (tutorialStep < (locks.Count - 1)) locks[index + 1].gameObject.SetActive(true);
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
            TutorialStateTransition(tutorialStep);
        }

        private void TutorialStateTransition(int index)
        {
            switch (tutorialStep)
            {
                case 0: //Gather
                    break;
                case 1: //Gather
                    dispenser_1.enabled = true;
                    HidePanel(0);
                    HidePanel(1);
                    HidePanel(2);
                    break;
                case 2: //Throttles
                    HidePanel(3);
                    break;
                case 3: //Engines
                    HidePanel(4);
                    HidePanel(5);
                    break;
                case 4: //Weapons
                    HidePanel(6);
                    break;
                case 5: //Items
                    dispenser_5.enabled = true;
                    HidePanel(7);
                    break;
                case 6: //Tools & Fire
                    throttle_6.parentCell.Ignite();
                    throttle_6.Break();
                    dispensers_6[0].enabled = true;
                    HidePanel(8);
                    break;
                case 7: //Uninstall
                    dispenser_7.enabled = true;
                    HidePanel(9);
                    break;
                case 8: //Garage
                    break;
            }
        }

        private void HidePanel(int index)
        {
            panelAnimators[index].Play("FadeOutPanel", 0, 0);
            
        }
    }
}
