using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellItemBehavior : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float chanceToExplode = 80;
    [SerializeField] private float chanceToCatchFire = 30;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private GameObject explosmoke;
    [SerializeField] private GameObject smallboom;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if((collision.collider.tag == "Layer" || collision.collider.tag == "OneWayPlatform") && GetComponent<DamageObject>() == null)
        {
            //If nothing is holding the shell, let it explode
            if(transform.parent.CompareTag("ItemContainer"))
            {
                ForceShellExplode();
                Instantiate(smallboom, transform.position, Quaternion.identity);
            }
        }
    }

    private void ForceShellExplode()
    {
        float generateChance = Random.Range(0, 100);
        if (generateChance < chanceToExplode)
        {
            gameObject.AddComponent<DamageObject>().damage = damage;
            Debug.Log("BOOM! Shell has exploded in tank.");
            if (FindObjectOfType<AudioManager>() != null)
                FindObjectOfType<AudioManager>().Play("ExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);
            Destroy(gameObject);
        }
    }

    public int GetDamage() => damage;
    public float GetChanceToCatchFire() => chanceToCatchFire;

    private void OnDestroy()
    {
        if(GetComponent<DamageObject>() != null)
        {
            Instantiate(explosionParticles, transform.position, Quaternion.identity);
            Instantiate(explosmoke, transform.position, Quaternion.identity);
            if (FindObjectOfType<AudioManager>() != null)
                FindObjectOfType<AudioManager>().Play("ExplosionSFX", PlayerPrefs.GetFloat("SFXVolume", GameSettings.defaultSFXVolume), gameObject);
        }
    }
}
