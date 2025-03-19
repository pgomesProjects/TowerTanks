using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class JobZone : InteractableZone
    {
        public Character.CharacterJobType jobType; //type of job associated with this zone
        public bool requiresItem; //if true, this zone requires an item to be interacted with
        public Cargo.CargoType requiredItem; //type of item required for this job
        public Transform jobSpot; //transform player is moved to for this job
        public bool showPrompt; //displays a UI prompt when a user is nearby

        private SymbolDisplay prompt;
        private ParticleSystem displayParticle;
        private Color defaultColor;

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            displayParticle = transform.parent.GetComponentInChildren<ParticleSystem>();
            if (displayParticle != null)
            {
                ParticleSystem.MainModule main = displayParticle.main;
                defaultColor = main.startColor.color;
            }
        }

        // Update is called once per frame
        protected override void Update()
        {
            base.Update();

            if (playerIsColliding)
            {
                if (showPrompt)
                {
                    //Get prompt colors
                    Color color = Color.white;
                    if (players.Count > 0) color = players[0].GetComponent<PlayerMovement>().GetCharacterColor();

                    if (prompt == null)
                    {
                        Vector2 pos = new Vector2(0, 1);
                        Sprite button = GameManager.Instance.buttonPromptSettings.GetPlatformPrompt(GameAction.Interact, PlatformType.Gamepad).PromptSprite;
                        
                        prompt = GameManager.Instance.UIManager.AddSymbolDisplay(this.gameObject, pos, button, "HOLD TO CLAIM", color);
                    }
                    else
                    {
                        prompt.transform.rotation = Quaternion.identity;
                    }

                    if (displayParticle != null)
                    {
                        ParticleSystem.MainModule main = displayParticle.main;
                        main.simulationSpeed = 2;
                        main.startColor = color;
                    }
                }
            }
            else if (prompt != null)
            {
                Destroy(prompt.gameObject);
                prompt = null;
            }

            if (!playerIsColliding)
            {
                if (displayParticle != null)
                {
                    ParticleSystem.MainModule main = displayParticle.main;
                    main.simulationSpeed = 1;
                    main.startColor = defaultColor;
                }
            }
        }

        public override void Interact(GameObject playerID)
        {
            base.Interact(playerID);
        }
    }
}
