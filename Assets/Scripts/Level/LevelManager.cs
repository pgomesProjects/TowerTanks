using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TowerTanks.Scripts.Deprecated;

namespace TowerTanks.Scripts
{
    public enum GAMESTATE
    {
        BUILDING, COMBAT, EVENT, GAMEOVER
    }

    public class LevelManager : SerializedMonoBehaviour
    {
        [Header("Objects & Components:")]
        [SerializeField] public TankController playerTank;
        [SerializeField] private Transform layerParent;
        [SerializeField] private Transform playerParent;
        [SerializeField] private GameObject layerPrefab;
        [SerializeField] private GameObject ghostLayerPrefab;
        [SerializeField, Tooltip("The component that tracks the objective information.")] private ObjectiveTracker objectiveTracker;
        [SerializeField, Tooltip("The component that tracks encounter & event information.")] public EventSpawnerManager eventManager;
        [SerializeField, Tooltip("The component that tracks tank information.")] public TankManager tankManager;

        [Header("Data:")]
        [SerializeField, Tooltip("List of possible level settings to use when generating a level")] public LevelSettings[] levelSettings;
        [SerializeField, Tooltip("The list of possible rooms for the players to pick.")] public GameObject[] roomList { get; private set; }
        [Tooltip("The value of a singular scrap piece.")] private int scrapValue;
        [SerializeField] public float enemiesDestroyed;
        public static float totalEnemiesDestroyed;
        public int enemyTierOffset;

        public static LevelManager Instance;

        internal bool readingTutorial;
        internal GAMESTATE levelPhase = GAMESTATE.BUILDING;
        internal WeatherConditions currentWeatherConditions;
        internal int currentRound;
        internal int totalLayers;
        internal bool isSettingUpOnStart;

        public static int totalScrapValue;

        private GameObject currentGhostLayer;

        private Transform spawnPoint;

        private Dictionary<string, int> itemPrice;

        //Events
        public static Action<LevelEvents> OnMissionStart;
        public static Action<LevelEvents> OnEnemyDefeated;
        public static Action<int, bool> OnResourcesUpdated;
        public static Action OnCombatEnded;
        public static Action OnGameOver;

        //Debug Tools
        [Button("Activate Test Tutorial")]
        private void DebugTestTutorial()
        {
            GameManager.Instance.DisplayTutorial(0, true);
        }

        [Button(ButtonSizes.Medium)]
        private void TestAddResources()
        {
            UpdateResources(resourcesToAdd);
        }

        [Button("Complete Mission")]
        private void AutoCompleteMission()
        {
            CompleteMission();
        }

        public int resourcesToAdd = 100;

        private void Awake()
        {
            Instance = this;
            readingTutorial = false;
            totalLayers = 1;
            currentRound = 0;
            itemPrice = new Dictionary<string, int>();
            PopulateItemDictionary();
            spawnPoint = playerParent.transform;
            tankManager = GameObject.Find("TankManager").GetComponent<TankManager>();
            playerTank = tankManager.tanks[0].gameObject.GetComponent<TankController>();
            eventManager = GetComponent<EventSpawnerManager>();
            //GameObject.FindGameObjectWithTag("SpawnPoint").transform;
        }

        private void Start()
        {
            isSettingUpOnStart = true;
            GameManager.Instance.AudioManager.Play("MainMenuWindAmbience");

            PickLevelMusic();

            /*        if (GameSettings.skipTutorial)
                    {
                        TransitionGameState();

                        AddLayer(); //Add another layer
                    }
                    else
                    {
                        totalScrapValue += 200;
                        GameObject.FindGameObjectWithTag("Resources").gameObject.SetActive(false);
                    }*/

            BuildPlayerTank();
            TransitionGameState();
            SpawnAllPlayers();
            //AddLayer(); //Add another layer

            if (GameSettings.debugMode)
                totalScrapValue = 99999;

            OnResourcesUpdated?.Invoke(totalScrapValue, false);
            OnMissionStart?.Invoke(CampaignManager.Instance.GetCurrentLevelEvent());
            isSettingUpOnStart = false;

            GameManager.Instance.DisplayTutorial(3, false, 5);
        }

        private void OnEnable()
        {
            GameManager.Instance.MultiplayerManager.OnPlayerConnected += SpawnPlayer;
            PlayerMovement.OnPlayerDeath += CheckForGameOver;
            ObjectiveTracker.OnMissionComplete += CompleteMission;
        }

        private void OnDisable()
        {
            GameManager.Instance.MultiplayerManager.OnPlayerConnected -= SpawnPlayer;
            PlayerMovement.OnPlayerDeath -= CheckForGameOver;
            ObjectiveTracker.OnMissionComplete -= CompleteMission;
        }

        private void BuildPlayerTank()
        {
            if (GameManager.Instance.tankDesign != null)
            {
                Debug.Log("Building Last Tank Saved...");
                playerTank.Build(GameManager.Instance.tankDesign);
            }
        }

        public void UpdatePlayerTank(TankController newTank)
        {
            playerTank = newTank;
            eventManager.UpdatePlayerTank();
            ChunkLoader.Instance.playerTank = newTank.treadSystem.transform;
        }

        private void SpawnAllPlayers()
        {
            playerParent = GameObject.FindGameObjectWithTag("PlayerContainer")?.transform;

            foreach (PlayerInput playerInput in GameManager.Instance.MultiplayerManager.GetPlayerInputs())
            {
                if (playerInput.playerIndex >= 0)
                    SpawnPlayer(playerInput);
            }
        }

        private void SpawnPlayer(PlayerInput playerInput)
        {
            Debug.Log("Spawning Player " + (playerInput.playerIndex + 1).ToString());

            spawnPoint = playerParent.transform;

            PlayerMovement character = Instantiate(GameManager.Instance.MultiplayerManager.GetPlayerPrefab());
            character.LinkPlayerInput(playerInput);
            MoveCharacterToSpawn(character);
            character.transform.GetComponentInChildren<Renderer>().material.SetColor("_Color", GameManager.Instance.MultiplayerManager.GetPlayerColors()[playerInput.playerIndex]);
            //character.SetPlayerMove(true);
            GameManager.Instance.AddCharacterHUD(character);
        }

        public void MoveCharacterToSpawn(Character characterObject)
        {
            Vector3 charPos = spawnPoint.position;
            charPos.x += UnityEngine.Random.Range(-0.25f, 0.25f);
            characterObject.transform.position = charPos;
        }

        public int GetEnemyTier()
        {
            int newInt = 1;

            if (totalEnemiesDestroyed > 1)
            {
                newInt = 2;
            }

            if (totalEnemiesDestroyed > 3)
            {
                newInt = 3;
            }

            if (totalEnemiesDestroyed > 5)
            {
                newInt = 4;
            }

            if (totalEnemiesDestroyed > 7)
            {
                newInt = 5;
            }

            return newInt + enemyTierOffset;
        }

        public LevelSettings GetLevelSettings(bool random = true, int index = 0)
        {
            LevelSettings settings = null;

            if (random)
            {
                //Get List of all possible Level Layouts depending on Enemy Tier
                List<LevelSettings> settingsPool = new List<LevelSettings>();
                int currentTier = GetEnemyTier();
                if (currentTier < 3) { settingsPool.Add(levelSettings[0]); } //Tier 1 & 2 only = +Desert Flat
                if (currentTier > 1 && currentTier < 5) { settingsPool.Add(levelSettings[1]); } //Tier 2-4 = +Desert Hills
                if (currentTier > 2) { settingsPool.Add(levelSettings[2]); } //Tier 3+ = +Desert City
                if (currentTier > 3) { settingsPool.Add(levelSettings[3]); } //Tier 4+ = +Desert Dangerous

                //Randomly pick one from the List
                int _random = UnityEngine.Random.Range(0, settingsPool.Count);
                settings = settingsPool[_random];

                settingsPool.Clear();
            }
            else
            {
                settings = levelSettings[index];
            }

            return settings;
        }

        public void PickLevelMusic()
        {
            int max = GetEnemyTier();
            if (max < 3) max = 2;
            int min = 0;
            if (max > 3) min = 1;

            int random = UnityEngine.Random.Range(min, max);
            if (random == 0) GameManager.Instance.AudioManager.Play("Mission_1", null, true);
            if (random == 1) GameManager.Instance.AudioManager.Play("Mission_2", null, true);
            if (random > 1) GameManager.Instance.AudioManager.Play("Mission_3", null, true);
        }

        /// <summary>
        /// Determines the price of the items the player can buy
        /// </summary>
        private void PopulateItemDictionary()
        {
            //Buy
            itemPrice.Add("NewLayer", 100);
        }

        /// <summary>
        /// Update the global scrap number.
        /// </summary>
        /// <param name="resources">If positive, scrap is gained. If negative, scrap is lost.</param>
        public void UpdateResources(int resources)
        {
            //Update the resources value and invoke the resources updated action
            if (!GameSettings.debugMode)
            {
                totalScrapValue += resources;
                OnResourcesUpdated?.Invoke(resources, true);
            }
        }

        public bool CanPlayerAfford(int price)
        {
            if (totalScrapValue >= price)
                return true;
            return false;
        }

        public bool CanPlayerAfford(string itemName)
        {
            if (totalScrapValue >= itemPrice[itemName])
                return true;
            return false;
        }

        public int GetItemPrice(string itemName) => itemPrice[itemName];

        public void AddCoalToTank(CoalController coalController, float amount)
        {
            //Add a percentage of the necessary coal to the furnace
            Debug.Log("Coal Has Been Added To The Furnace!");
            coalController.AddCoal(amount);
        }

        public void PurchaseLayer(PlayerController playerBuilding)
        {
            //Purchase a layer
            AddLayer();
            RemoveGhostLayer();
            //GetPlayerTank().GetLayerAt(playerBuilding.currentLayer).GetComponent<GhostInteractables>().CreateGhostInteractables(playerBuilding);
        }

        private void AddLayer()
        {
            //Spawn a new layer and adjust it to go inside of the tank parent object
            GameObject newLayer = Instantiate(layerPrefab);
            newLayer.transform.parent = layerParent;
            newLayer.transform.localPosition = new Vector2(0, totalLayers * 8);

            //Add to the total number of layers and give the new layer an index
            totalLayers++;
            newLayer.name = "TANK LAYER " + totalLayers;
            newLayer.GetComponentInChildren<LayerTransitionManager>().SetLayerIndex(totalLayers);

            //Adjust the top view of the tank
            AdjustCameraPosition();

            //Add layer to the list of layers
            //playerTank.GetLayers().Add(newLayer.GetComponent<LayerManager>());

            if (!isSettingUpOnStart)
            {
                //Check interactables on layer
                //CheckInteractablesOnLayer(totalLayers);
                //Play sound effect
                GameManager.Instance.AudioManager.Play("UseSFX");
            }

            //Adjust the weight of the tank
            //playerTank.AdjustTankWeight(totalLayers);

            //Adjust the outside of the tank
            //playerTank.AdjustOutsideLayerObjects();
        }

        /// <summary>
        /// Moves the anchor on top of the tank so that the camera can view the entire tank.
        /// </summary>
        private void AdjustCameraPosition()
        {
            if (totalLayers > 2)
                playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(0, 4 + (totalLayers * 4) + ((totalLayers - 2) * 1.5f));
            else
                playerTank.transform.Find("TankFollowTop").transform.localPosition = new Vector2(0, 4);
        }

        public void AddGhostLayer()
        {
            //Spawn a new layer and adjust it to go inside of the tank parent object
            if (currentGhostLayer == null)
            {
                currentGhostLayer = Instantiate(ghostLayerPrefab);
                currentGhostLayer.transform.parent = playerTank.transform;
                currentGhostLayer.transform.localPosition = new Vector2(0, totalLayers * 8);
            }
            //If there's already a ghost layer but it's inactive, activate it
            else if (!currentGhostLayer.activeInHierarchy)
                currentGhostLayer.SetActive(true);
        }

        public void HideGhostLayer()
        {
            if (currentGhostLayer != null)
                currentGhostLayer.SetActive(false);
        }

        public void RemoveGhostLayer() => Destroy(currentGhostLayer);

        /// <summary>
        /// Cancels repairs for all players.
        /// </summary>
        public void CancelAllLayerRepairs()
        {
            foreach (var player in FindObjectsOfType<PlayerController>())
                player.CancelLayerRepair();
        }

        public void AdjustLayerSystem(int destroyedLayer)
        {
            //If there are no more layers, the game is over
            if (totalLayers == 0)
            {
                Debug.Log("Tank Is Destroyed!");
                //playerTank.DestroyTank();
                //Switch from gameplay to game over
                TransitionGameState();
                return;
            }

            //Adjust the layer numbers for the layers above the one that got destroyed
            foreach (var i in playerTank.GetComponentsInChildren<LayerManager>())
            {
                int nextLayerIndex = i.GetComponentInChildren<LayerTransitionManager>().GetNextLayerIndex();

                if (nextLayerIndex > destroyedLayer + 1)
                {
                    i.GetComponentInChildren<LayerTransitionManager>().SetLayerIndex(nextLayerIndex - 1);
                    i.UnlockAllInteractables();
                }
            }

            //Adjust the top view of the tank
            AdjustCameraPosition();

            //Adjust the ghost layer if active
            if (currentGhostLayer != null)
            {
                currentGhostLayer.transform.localPosition = new Vector2(0, totalLayers * 8);
            }

            //Adjust the players's layer numbers
            foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
            {
                //If the player is on or above the destroyed layer
                if (player.GetComponent<PlayerController>().currentLayer >= destroyedLayer)
                {
                    //If the player is not on the outside of the tank
                    if (player.GetComponent<PlayerController>().currentLayer != totalLayers)
                    {
                        //Decrement the layer number
                        player.GetComponent<PlayerController>().currentLayer--;
                    }
                }

                //If the player is trying to repair on this layer, cancel the action
                player.GetComponent<PlayerController>().CancelLayerRepair();
            }

            //Adjust the weight of the tank
            //playerTank.AdjustTankWeight(totalLayers);
        }

        public void ResetPlayerCamera()
        {
            Debug.Log("Resetting Camera...");
            StartCoroutine(CameraEventController.Instance.BringCameraToPlayer(2));
        }

        public void EnemyDestroyed()
        {
            EnemySpawnManager enemySpawn = FindObjectOfType<EnemySpawnManager>();

            if (enemySpawn != null && enemySpawn.AllEnemiesGone())
            {
                enemySpawn.enemySpawnerActive = false;
                PrepareBeforeCombat();
            }
        }

        public void PrepareBeforeCombat()
        {
            //GetPlayerTank()?.ResetTankDistance();
            OnCombatEnded?.Invoke();
        }

        public void TransitionGameState()
        {
            /*        switch (levelPhase)
                    {
                        //Tutorial to Gameplay
                        case GAMESTATE.TUTORIAL:
                            levelPhase = GAMESTATE.GAMEACTIVE;
                            tutorialPopup.SetActive(false);
                            readingTutorial = false;
                            PrepareBeforeCombat();
                            break;
                        //Gameplay to Game Over
                        case GAMESTATE.GAMEACTIVE:
                            levelPhase = GAMESTATE.GAMEOVER;
                            CameraEventController.Instance.FreezeCamera();
                            GameOver();
                            break;
                    }*/
        }

        public void CheckForGameOver()
        {
            //Check if any players are alive
            foreach (PlayerMovement player in FindObjectsOfType<PlayerMovement>())
            {
                if (!player.IsPermanentDead())
                    return;
            }

            //If no players are alive, it is game over
            GameOver();
        }

        public void StartCombatMusic(int layers)
        {
            if (!GameManager.Instance.AudioManager.IsPlaying("CombatMusic"))
                GameManager.Instance.AudioManager.Play("CombatMusic");

            //Decides how many layers of music should play depending on the amount of enemy layers
            int musicLayers;
            if (layers >= 7)
                musicLayers = 4;

            else if (layers >= 5)
                musicLayers = 3;

            else if (layers >= 3)
                musicLayers = 2;

            else
                musicLayers = 1;

            //Set layers that are playing to full volume while muting layers that are not playing
            for (int i = 0; i < 4; i++)
            {
                if (i + 1 <= musicLayers)
                    AkSoundEngine.SetRTPCValue("CombatLayer" + i + "Volume", 100f);
                else
                    AkSoundEngine.SetRTPCValue("CombatLayer" + i + "Volume", 0f);
            }

            AkSoundEngine.SetRTPCValue("GlobalCombatVolume", 100, GameManager.Instance.AudioManager.GlobalGameObject);
        }

        /// <summary>
        /// Fades out and stops the combat music.
        /// </summary>
        /// <param name="fadeDuration">The duration of the fade (in seconds).</param>
        /// <returns></returns>
        public IEnumerator StopCombatMusic(float fadeDuration)
        {
            AkSoundEngine.SetRTPCValue("GlobalCombatVolume", 0, GameManager.Instance.AudioManager.GlobalGameObject, (int)(fadeDuration * 1000f));
            yield return new WaitForSeconds(fadeDuration);

            if (GameManager.Instance.AudioManager != null)
            {
                if (GameManager.Instance.AudioManager.IsPlaying("CombatMusic"))
                    GameManager.Instance.AudioManager.Stop("CombatMusic");
            }
        }

        public void GameOver()
        {
            levelPhase = GAMESTATE.GAMEOVER;

            //Stop all of the in-game sounds
            GameManager.Instance.AudioManager.StopAllSounds();

            //Destroy all particles
            foreach (var particle in FindObjectsOfType<ParticleSystem>())
                Destroy(particle.gameObject);

            //Stop all coroutines
            StopAllCoroutines();

            GameManager.Instance.AudioManager.Play("DeathStinger");

            OnGameOver?.Invoke();
        }

        public void AddObjectiveValue(ObjectiveType type, float amount)
        {
            if (CampaignManager.Instance.GetCurrentLevelEvent().objectiveType == type)
            {
                if (type == ObjectiveType.DefeatEnemies)
                {
                    enemiesDestroyed += amount;
                    totalEnemiesDestroyed += amount;
                    OnEnemyDefeated?.Invoke(CampaignManager.Instance.GetCurrentLevelEvent());
                    StartCoroutine(ConditionChecker());
                }
            }
        }

        private IEnumerator ConditionChecker()
        {
            yield return new WaitForSeconds(3f);
            CheckLevelConditions();
        }

        private void CheckLevelConditions()
        {
            if (CampaignManager.Instance.GetCurrentLevelEvent().objectiveType == ObjectiveType.DefeatEnemies)
            {
                if (enemiesDestroyed >= CampaignManager.Instance.GetCurrentLevelEvent().enemiesToDefeat)
                {
                    //Spawn the End Level Flag
                    eventManager.SpawnEndOfLevel();

                    //Display tutorial
                    GameManager.Instance.DisplayTutorial(5, false, 5);
                }
            }
        }

        public void CompleteMission()
        {
            TankDesign currentTankDesign = playerTank.GetCurrentDesign();
            CargoManifest manifest = playerTank.GetCurrentManifest();
            GameManager.Instance.tankDesign = currentTankDesign;
            GameManager.Instance.cargoManifest = manifest;
            GameManager.Instance.LoadScene("BuildTankScene", LevelTransition.LevelTransitionType.GATE, true, true, false);
            GameManager.shopChance = eventManager.shopChance;
        }

        public int GetScrapValue() => scrapValue;
        public TankController GetPlayerTank() => playerTank;

        private void OnDestroy()
        {
            //Scene cleanup
            foreach (var particle in FindObjectsOfType<ParticleSystem>())
                Destroy(particle);
        }
    }

}