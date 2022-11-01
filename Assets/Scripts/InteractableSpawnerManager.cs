using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InteractableSpawnerManager : MonoBehaviour
{
    [SerializeField] private GameObject cannon;
    [SerializeField] private GameObject engine;
    [SerializeField] private GameObject shellStation;
    [SerializeField] private GameObject throttle;

    [SerializeField] private GameObject[] ghostInteractables;

    private void Start()
    {
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
        GameObject newGhost = Instantiate(ghostInteractables[currentSpawner.GetCurrentGhostIndex()], ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.position, currentSpawner.transform.rotation);
        newGhost.transform.parent = currentSpawner.transform;
        newGhost.transform.localPosition = ghostInteractables[currentSpawner.GetCurrentGhostIndex()].transform.localPosition;
    }

    public void UpdateGhostInteractable(InteractableSpawner currentSpawner, int index)
    {
        Destroy(currentSpawner.transform.GetChild(0).gameObject);
        currentSpawner.UpdateGhostIndex(index, ghostInteractables.Length);
        ShowNewGhostInteractable(currentSpawner);
    }
}
