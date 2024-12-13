using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class FlagSettings : MonoBehaviour
    {
        public SpriteRenderer renderer;
        public Sprite flagSprite;

        public float windIntensity;
        public float windSpeed;

        public void Awake()
        {
            renderer.material.SetFloat("_WindIntensity", windIntensity);
            renderer.material.SetFloat("_WindSpeed", windSpeed);
        }

        public void Start()
        {
            renderer.sprite = flagSprite;
        }
    }
}
