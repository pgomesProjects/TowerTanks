using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellItemBehavior : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float chanceToExplode = 80;
    [SerializeField] private float chanceToCatchFire = 30;
    [SerializeField] private ParticleSystem explosionParticles;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if((collision.collider.tag == "Layer" || collision.collider.tag == "OneWayPlatform") && GetComponent<DamageObject>() == null)
        {
            float generateChance = Random.Range(0, 100);
            if(generateChance < chanceToExplode)
            {
                gameObject.AddComponent<DamageObject>().damage = damage;
                Debug.Log("BOOM! Shell has exploded in tank.");
                Destroy(gameObject);
            }
        }
    }

    public int GetDamage() => damage;
    public float GetChanceToCatchFire() => chanceToCatchFire;

    private void OnDestroy()
    {
        if(GetComponent<DamageObject>() != null)
        {
            Instantiate(explosionParticles, transform.position, Quaternion.identity);
            if(FindObjectOfType<AudioManager>() != null)
                FindObjectOfType<AudioManager>().PlayOneShot("ExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        }
    }
}