using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class CargoMelee : Cargo
    {
        [Header("Melee Settings:")]
        public bool isWrench;
        [Tooltip("Damage dealt from a single hit of this weapon.")] public float meleeDamage;
        [Tooltip("Cooldown between when this weapon can be used.")] public float meleeCooldown;
        [Tooltip("Radius of the hitbox of this weapon.")] public float hitBoxRadius;
        [Tooltip("Centerpoint of hitbox of this weapon.")] public Transform hitBoxPivot;
        [Tooltip("LayerMask of objects we can hit with this weapon.")] public LayerMask hitMask;
        [Tooltip("Durability of this object - breaks when it hits 0.")] public int durability;
        private float meleeCooldownTimer = 0;
        private Animator meleeAnimator;
        private bool canSwing;

        protected override void Awake()
        {
            base.Awake();

            meleeAnimator = GetComponent<Animator>();
            
        }

        protected override void Start()
        {
            base.Start();

            canSwing = true;
        }

        protected override void Update()
        {
            base.Update();
        }

        private void FixedUpdate()
        {
            if (meleeCooldownTimer > 0)
            {
                meleeCooldownTimer -= Time.fixedDeltaTime;
                if (meleeCooldownTimer <= 0)
                {
                    meleeCooldownTimer = 0;
                }
            }
            else if (!canSwing) canSwing = true;
        }

        public override void Use(bool held = false)
        {
            base.Use();

            //GameManager.Instance.AudioManager.Play("UseWrench", this.gameObject);
            Swing();
        }

        public void Swing()
        {
            if (canSwing)
            {
                meleeCooldownTimer = meleeCooldown;
                meleeAnimator.Play("WrenchSwing", 0, 0);
                canSwing = false;
            }
        }

        public void CheckMeleeHit()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitBoxPivot.position, hitBoxRadius, hitMask);
            if (hits.Length > 0)
            {
                foreach (Collider2D hit in hits)
                {
                    Cell cell = hit.gameObject.GetComponent<Cell>();
                    TreadSystem treads = hit.gameObject.GetComponentInParent<TreadSystem>();
                    if (isWrench)
                    {
                        if (cell != null)
                        {
                            float amountRepaired = cell.Repair(25);
                            durability -= (int)amountRepaired;
                        }
                    }
                    else
                    {
                        if (hit.gameObject.GetComponent<Character>() == currentHolder) break; //Don't hit yourself

                        //Handle melee damage:
                        IDamageable target = hit.GetComponent<IDamageable>();                 //Try to get damage receipt component from collider object
                        if (target == null) target = hit.GetComponentInParent<IDamageable>(); //If damage receipt component is not in collider object, look in parent objects
                        if (target != null) //Tool has hit a target
                        {
                            //damagedThisHit.Add(target);                                //Indicate that target is being damaged now so it is not hit by splash damage later
                            float damage = meleeDamage;
                            if (cell != null || treads != null)
                            {
                                GameManager.Instance.AudioManager.Play("TankImpact", this.gameObject);
                                damage *= 3f;
                            }

                            float damageDealt = target.Damage(damage, true);             //Strike target & return damage dealt
                            durability -= (int)damageDealt;                              //Subtract damage dealt from durability of tool
                        }
                    }

                    //Check Durability
                    if (durability <= 0)
                    {
                        GameManager.Instance.AudioManager.Play("TankImpact", this.gameObject);
                        Destroy(this.gameObject);
                    }
                }
            }
        }

        public override void AssignValue(int value)
        {
            base.AssignValue(value);

            float defaultValue = durability;
            int defaultAmount = amount;

            if (value == -1) return;
            durability = value;

            amount = Mathf.RoundToInt(defaultAmount * (durability / defaultValue)); //scale sale price based on how 'used' this object is
        }

        public override int GetPersistentValue()
        {
            int value = durability;

            return value;
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hitBoxPivot.position, hitBoxRadius);
        }
    }
}
