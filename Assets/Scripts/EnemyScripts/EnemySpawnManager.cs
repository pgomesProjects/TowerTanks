using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    private List<int> enemyTypeCounters;

    private void Start()
    {
        enemyTypeCounters = new List<int>();
    }

    private IEnumerator enemyEncounterAni;
    private void SpawnRandomEnemy()
    {
        //Pick a random enemy from the list of enemies and spawn it
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject newEnemy = Instantiate(enemyPrefabs[enemyIndex], transform.position, transform.rotation);


        Vector3 cameraZoomPos = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().transform.position;

        if (enemyEncounterAni != null)
            StopCoroutine(enemyEncounterAni);

        enemyEncounterAni = CameraEventController.instance.ShowEnemyWithCamera(40, new Vector3(cameraZoomPos.x + 55, cameraZoomPos.y, cameraZoomPos.z), 3, newEnemy);

        StartCoroutine(enemyEncounterAni);

        if(!FindObjectOfType<AudioManager>().IsPlaying("CombatOST"))
            FindObjectOfType<AudioManager>().Play("CombatOST", PlayerPrefs.GetFloat("BGMVolume", 0.5f));

        LevelManager.instance.currentRound++;

        Debug.Log("===ENEMY #" + LevelManager.instance.currentRound + " HAS SPAWNED!===");
    }

    public void GetReadyForEnemySpawn()
    {
        if(FindObjectOfType<AudioManager>() != null)
        {
            if (FindObjectOfType<AudioManager>().IsPlaying("CombatOST"))
                FindObjectOfType<AudioManager>().Stop("CombatOST");
        }

        int randomTime = Random.Range(8, 13);

        Invoke("SpawnRandomEnemy", randomTime);
    }
}
