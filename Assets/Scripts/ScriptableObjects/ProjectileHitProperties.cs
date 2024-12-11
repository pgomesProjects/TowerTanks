using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TowerTanks.Scripts
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ProjectileHitProperties")]
    public class ProjectileHitProperties : ScriptableObject
    {
        [Header("Base Values:")]
        [Tooltip("Damage dealt on direct hit."), Min(0)]                                        public float damage;
        [Tooltip("Mass of this projectile (affects how much impact force it exerts)."), Min(0)] public float mass;
        [Tooltip("If true, this projectile utilizes splash damage")]                            public bool hasSplashDamage; //Whether or not this projectile deals splash damage
        [Tooltip("Defines characteristics of splash damage zones"), ShowIf("hasSplashDamage")]  public SplashData[] splashData;
        [Header("Additional Projectile Abilities:")]
        [Tooltip("If true, this projectile will penetrate through multiple cells until its total damage value is used up.")]         public bool tunnels;
        [Tooltip("Chance this projectile lights things on fire when dealing damage"), Range(0, 1)]                                   public float fireChance;
        [Tooltip("If true, projectile will deal a normal amount of damage to armor.")]                                               public bool ignoresArmor;
        [Tooltip("Additional non-torque-inducing force applied to tanks by this projectile (used to push enemies around)."), Min(0)] public float slamForce;
    }
}
