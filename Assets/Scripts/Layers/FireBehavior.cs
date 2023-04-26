using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireBehavior : MonoBehaviour
{
    [SerializeField, Tooltip("The amount of time it takes for the fire to deal damage.")] private float fireTickSeconds;
    [SerializeField] private int damagePerTick = 5;
    [SerializeField, Tooltip("The multiplier for the speed that the fire takes to be put out when more players put out the fire.")] private float multiplierSpeed;
    public GameObject fireParticle;
    private GameObject[] currentParticles;
    private float currentTimer;
    private bool layerOnFire;
    private List<PlayerController> playersPuttingOutFire = new List<PlayerController>();

    private void OnEnable()
    {
        CreateFires(1);
        layerOnFire = true;
        currentTimer = 0;
        FindObjectOfType<AudioManager>().Play("FireBurningSFX", gameObject);
    }

    private void OnDisable()
    {
        layerOnFire = false;
        if(LevelManager.instance.levelPhase == GAMESTATE.TUTORIAL)
        {
            if(TutorialController.main.currentTutorialState == TUTORIALSTATE.PUTOUTFIRE)
            {
                TutorialController.main.OnTutorialTaskCompletion();
            }
        }

        if(FindObjectOfType<AudioManager>() != null)
            FindObjectOfType<AudioManager>().Stop("FireBurningSFX", gameObject);

        playersPuttingOutFire.Clear();
    }

    private void CreateFires(int firesToCreate)
    {
        float randomX = Random.Range(-5f, 5f);
        float randomY = Random.Range(-2f, 2f);

        for (int f = 0; f < firesToCreate; f++)
        {
            var childFire = Instantiate(fireParticle, new Vector3(transform.position.x + randomX, transform.position.y + randomY, transform.position.z), Quaternion.identity, this.transform);
            childFire.transform.localScale = Vector3.one;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (layerOnFire)
        {
            currentTimer += Time.deltaTime;

            if(currentTimer > fireTickSeconds)
            {
                if(LevelManager.instance.levelPhase == GAMESTATE.GAMEACTIVE)
                    GetComponentInParent<LayerManager>().DealDamage(damagePerTick, false);
                currentTimer = 0;
            }
        }
    }

    public void AddPlayerPuttingOutFire(PlayerController currentPlayer)
    {
        if (!playersPuttingOutFire.Contains(currentPlayer))
        {
            playersPuttingOutFire.Add(currentPlayer);
            UpdatePlayerActionSpeed();
        }
    }

    public void RemovePlayerPuttingOutFire(PlayerController currentPlayer)
    {
        if (playersPuttingOutFire.Contains(currentPlayer))
        {
            playersPuttingOutFire.Remove(currentPlayer);
            UpdatePlayerActionSpeed();
        }
    }

    private void UpdatePlayerActionSpeed()
    {
        float currentMultiplier = 1f;
        for (int i = 1; i < playersPuttingOutFire.Count; i++)
            currentMultiplier *= multiplierSpeed;

        foreach(var player in playersPuttingOutFire)
            player.SetActionSpeed(player.GetFireRemoverSpeed() * currentMultiplier);
    }

    public bool IsLayerOnFire() => layerOnFire;
}
