using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class AnimatorParameterAssigner : MonoBehaviour
    {
        private Animator animator;
        public string parameter;
        public bool boolSetting;

        public void Awake()
        {
            animator = GetComponent<Animator>();
            animator.SetBool(parameter, boolSetting);
        }
    }
}
