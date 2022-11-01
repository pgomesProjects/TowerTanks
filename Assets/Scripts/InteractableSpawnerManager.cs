using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableSpawnerManager : MonoBehaviour
{
    [SerializeField] private GameObject cannon;
    [SerializeField] private GameObject engine;
    [SerializeField] private GameObject shellStation;
    [SerializeField] private GameObject throttle;

    [SerializeField] private GameObject[] ghostInteractables;
    private int currentGhostInteractable;

    private void Start()
    {
        currentGhostInteractable = 0;
    }

    public void SpawnCannon(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(cannon);
    }

    public void SpawnEngine(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(engine);
    }

    public void SpawnShellStation(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(shellStation);
    }

    public void SpawnThrottle(InteractableSpawner currentSpawner)
    {
        currentSpawner.SpawnInteractable(throttle);
    }

    public void ShowNewGhostInteractable(InteractableSpawner currentSpawner)
    {
        GameObject newGhost = Instantiate(ghostInteractables[currentGhostInteractable], ghostInteractables[currentGhostInteractable].transform.position, currentSpawner.transform.rotation);
        newGhost.transform.parent = currentSpawner.transform;
        newGhost.transform.localPosition = ghostInteractables[currentGhostInteractable].transform.localPosition;
    }
}
