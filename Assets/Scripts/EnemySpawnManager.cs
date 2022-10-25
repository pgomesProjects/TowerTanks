using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class EnemySpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;

    // Start is called before the first frame update
    void Start()
    {
        //Spawn an enemy initially
        Invoke("SpawnRandomEnemy", 5);
    }

    private void SpawnRandomEnemy()
    {
        //Pick a random enemy from the list of enemies and spawn it
        int enemyIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject newEnemy = Instantiate(enemyPrefabs[enemyIndex], transform.position, transform.rotation);

        Vector3 cameraZoomPos = GameObject.FindGameObjectWithTag("PlayerTank").GetComponent<PlayerTankController>().transform.position;

        StartCoroutine(CameraEventController.instance.ShowEnemyWithCamera(20, new Vector3(cameraZoomPos.x + 20, cameraZoomPos.y, cameraZoomPos.z), 3));

        FindObjectOfType<AudioManager>().Play("CombatOST", PlayerPrefs.GetFloat("BGMVolume", 0.5f));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
