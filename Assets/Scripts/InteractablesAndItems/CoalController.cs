using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts.Deprecated
{
    public class CoalController : InteractableController
    {
        [SerializeField] [Tooltip("The amount of seconds it takes for the coal to fully deplete")] private float depletionSeconds;
        [SerializeField] private Slider coalPercentageIndicator;
        [SerializeField] private int framesForCoalFill = 5;
        [SerializeField] private float angleRange = 102;
        [SerializeField] private float coalShovelCooldown = 0.1f;

        [SerializeField] private GameObject sparks;

        private int currentCoalFrame;
        private float currentIndicatorAngle;
        private float depletionRate;
        private float coalPercentage;
        private bool hasCoal;
        private Animator engineAnimator;
        private float sparkTimer = 0f;

        public Sprite[] coalAnimationSprites;
        private float coalLoadAudioLength = 1.5f;
        private float currentCooldown;
        private bool canShovel;

        private Transform indicatorPivot;

        // Start is called before the first frame update
        void Start()
        {
            engineAnimator = GetComponentInChildren<Animator>();

            coalPercentage = 100f;
            hasCoal = true;

            currentCoalFrame = 0;
            depletionRate = depletionSeconds / 100f;
            coalPercentageIndicator.value = coalPercentage;
            indicatorPivot = transform.Find("IndicatorPivot");
            canShovel = true;

            FindObjectOfType<PlayerTankController>().AdjustEngineSpeedMultiplier();

            AdjustIndicatorAngle();
        }

        // Update is called once per frame
        void Update()
        {
            engineAnimator.SetFloat("Percentage", coalPercentage);

            //If there is coal, use it
            if (hasCoal)
            {
                CoalDepletion();
            }
            else
            {
                if (sparkTimer > 2.0f)
                {
                    sparkTimer = 0f;
                    Instantiate(sparks, transform.position, Quaternion.identity);
                }
                sparkTimer += Time.deltaTime;
            }

            if (!canShovel)
            {
                if (currentCooldown > coalShovelCooldown)
                {
                    canShovel = true;
                }
                else
                    currentCooldown += Time.deltaTime;
            }
        }

        /// <summary>
        /// Fills coal so that the tank can continue moving
        /// </summary>
        public void StartCoalFill()
        {
            //If there is a player
            if (currentPlayer != null)
            {
                currentCoalFrame = 0;
            }
        }

        /// <summary>
        /// Progresses the coal fill when used.
        /// </summary>
        public override void OnUseInteractable()
        {
            if (IsInteractionActive() && canShovel)
                ProgressCoalFill();
        }

        /// <summary>
        /// Progresses the coal fill meter.
        /// </summary>
        private void ProgressCoalFill()
        {
            //If there is a player locked in
            if (currentPlayerLockedIn != null)
            {
                currentPlayer.AddToProgressBar(100f / framesForCoalFill);
                currentCoalFrame++;
                StartCoroutine(AnimateCoalFill());

                AudioManager audio = GameManager.Instance.AudioManager;
                float totalAudioTime = audio.GetSoundLength("LoadingCoal");

                float startAudioTime;
                float endAudioTime;

                startAudioTime = (coalLoadAudioLength / framesForCoalFill) * (currentCoalFrame - 1);
                endAudioTime = (coalLoadAudioLength / framesForCoalFill) * currentCoalFrame;

                Debug.Log("Total Audio Time: " + totalAudioTime);

                Debug.Log("Start Audio Time: " + startAudioTime);
                Debug.Log("End Audio Time: " + endAudioTime);

                if (currentPlayer.IsProgressBarFull())
                {
                    audio.Play("LoadingCoal", gameObject);
                    FillCoal(15f);
                    currentPlayer.ShowProgressBar();
                    currentCoalFrame = 0;

                    startAudioTime = coalLoadAudioLength;
                    endAudioTime = totalAudioTime;
                }

                audio.PlayAtSection("LoadingCoal", startAudioTime, endAudioTime);

                canShovel = false;
                currentCooldown = 0f;
            }
        }

        private void FillCoal(float percent)
        {
            //Add a percentage of the necessary coal to the furnace
            Debug.Log("Coal Has Been Added To The Furnace!");
            AddCoal(percent);
        }

        /// <summary>
        /// Cancels the filling of coal when the player lets go of the interact button
        /// </summary>
        public void CancelCoalFill()
        {
            //If there is a player
            if (currentPlayer != null)
            {
                LockPlayer(currentPlayer, false);
                currentCoalFrame = 0;
            }
        }

        public IEnumerator AnimateCoalFill()
        {
            SpriteRenderer playerSprite = currentPlayer.GetComponent<SpriteRenderer>();
            int frameToAnimate = currentCoalFrame;
            playerSprite.sprite = coalAnimationSprites[2 * (frameToAnimate - 1)];

            yield return new WaitForSeconds(4f / 60f);
            playerSprite.sprite = coalAnimationSprites[2 * (frameToAnimate - 1) + 1];

        }

        public override void LockPlayer(PlayerController currentPlayer, bool lockPlayer)
        {
            base.LockPlayer(currentPlayer, lockPlayer);

            if (lockPlayer)
            {
                currentPlayer.ShowProgressBar();
                currentPlayer.gameObject.GetComponent<Animator>().enabled = false;
                currentPlayer.gameObject.GetComponent<SpriteRenderer>().sprite = coalAnimationSprites[0];
            }
            else
            {
                currentPlayer.HideProgressBar();
                currentPlayer.gameObject.GetComponent<Animator>().enabled = true;
            }
        }

        public override void UnlockAllPlayers()
        {
            base.UnlockAllPlayers();

            currentPlayer.HideProgressBar();
            currentPlayer.gameObject.GetComponent<Animator>().enabled = true;
        }

        private void AdjustIndicatorAngle()
        {
            currentIndicatorAngle = -angleRange + ((angleRange * 2f) - (angleRange * 2f * (coalPercentage / 100f)));
            currentIndicatorAngle = Mathf.Clamp(currentIndicatorAngle, -angleRange, angleRange);

            indicatorPivot.eulerAngles = new Vector3(0, 0, currentIndicatorAngle);
        }

        private void CoalDepletion()
        {
            if (GameSettings.debugMode)
                return;

            //If the player has not gotten a game over, deplete coal
            if (LevelManager.Instance.levelPhase != GAMESTATE.GAMEOVER)
            {
                //If the coal percentage is greater than 0, constantly deplete it
                if (coalPercentage > 0f)
                {
                    coalPercentage -= (1f / depletionRate) * Time.deltaTime;
                    //coalPercentageIndicator.value = coalPercentage;
                    hasCoal = true;
                }
                else
                {
                    Debug.Log("Coal Is Out!");
                    hasCoal = false;
                    //LevelManager.Instance.GetPlayerTank().AdjustEngineSpeedMultiplier();
                    GameManager.Instance.AudioManager.Play("EngineDyingSFX", gameObject);
                    Instantiate(sparks, transform.position, Quaternion.identity);
                }

                AdjustIndicatorAngle();
            }
        }

        public void AddCoal(float addToCoal)
        {
            //Add to the coal percentage
            coalPercentage += addToCoal;

            //If there is now coal, make sure to update the coal
            if (coalPercentage > 0f)
            {
                hasCoal = true;
                //LevelManager.Instance.GetPlayerTank().AdjustEngineSpeedMultiplier();
            }

            //Make sure the coal percentage does not pass 100%
            if (coalPercentage > 100f)
            {
                coalPercentage = 100f;
            }

            AdjustIndicatorAngle();
        }

        public bool HasCoal() => hasCoal;

        public bool IsCoalFull() => coalPercentage >= 100f;

        private void OnDestroy()
        {
            hasCoal = false;
            currentCoalFrame = 0;
            if (GameObject.FindGameObjectWithTag("PlayerTank"))
            {
                //LevelManager.Instance.GetPlayerTank().AdjustEngineSpeedMultiplier();
            }

            if (currentPlayerLockedIn != null && lockPlayerIntoInteraction)
            {
                CancelCoalFill();
            }
        }
    }
}
