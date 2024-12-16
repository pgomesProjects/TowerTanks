using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class MagnetController : TankInteractable
    {
        //Objects & Components:
        [Tooltip("Joint around which gimballed assembly rotates."), SerializeField] private Transform pivot;
        [Tooltip("Visual for magnet beam."), SerializeField]                        private Transform beam;

        //Settings:
        [Header("Gimbal Settings:")]
        [Tooltip("Speed at which the cannon barrel rotates"), SerializeField, Min(0)]               private float rotateSpeed;
        [Tooltip("Max angle (up or down) weapon joint can be rotated to."), SerializeField, Min(0)] private float gimbalRange;

        [Header("Magnet Settings:")]
        [Tooltip("How far beam extends from barrel."), SerializeField, Min(0)]                                                              private float beamLength;
        [Tooltip("Cross-sectional area of beam."), SerializeField, Min(0)]                                                                  private float beamWidth;
        [Tooltip("Layers which can be hit by beam."), SerializeField]                                                                       private LayerMask hitLayers;
        [Tooltip("Force (in units of acceleration per second per second) applied by objects caught in the magnet's beam."), SerializeField] private float magnetPower;

        //Runtime Variables:
        private bool active = false;      //True when magnet beam is active
        private float pivotRotation = 0;  //Current rotation of gimbal

        //RUNTIME METHODS:
        private void Update()
        {
            //Projectile magnetization:
            if (active) //Beam is active
            {
                RaycastHit2D[] hits = Physics2D.BoxCastAll(beam.position + (beam.right * (beamWidth / 2)), Vector2.one * beamWidth, 0, beam.right, beamLength - beamWidth, hitLayers); //Boxcast for all objects on target layers within beam
                foreach (RaycastHit2D hit in hits) //Iterate through objects caught in magnet
                {
                    print("object magnetized");
                    IMagnetizable magnetizedObject = null;                                                                             //Create container to store magnetization target script
                    magnetizedObject = hit.transform.gameObject.GetComponent<IMagnetizable>();                                         //Try to get magnetizable component
                    if (magnetizedObject == null) magnetizedObject = hit.transform.gameObject.GetComponentInChildren<IMagnetizable>(); //If the last check failed, look in children for magnetizable object
                    if (magnetizedObject != null) //Hit object is magnetizable
                    {
                        magnetizedObject.ApplyMagnetForce((magnetPower * Time.deltaTime) * beam.right, hit.point);
                    }
                }
            }
        }

        //INTERACTABLE METHODS:
        public override void Use(bool overrideConditions = false)
        {
            if (cooldown <= 0) //Prevent beam from activating as soon as player steps on board
            {
                active = true;                   //Indicate that beam is now turned on
                beam.gameObject.SetActive(true); //Activate beam object
                SetBeamLength(beamLength);       //Set beam length to target value
                SetBeamWidth(beamWidth);         //Set beam width to target value
            }
        }
        public override void CancelUse()
        {
            active = false;                   //Indicate that beam is now turned off
            beam.gameObject.SetActive(false); //Deactivate beam object
            SetBeamLength(0);                 //Set beam length to zero
        }
        public override void Rotate(float force)
        {
            //Rotate gimbal:
            float speed = rotateSpeed * Time.deltaTime * force * direction;                //Calculate number of degrees by which gimbal will be rotated (also apply direction and force values from base interactable and input parameter)
            pivotRotation = Mathf.Clamp(pivotRotation + speed, -gimbalRange, gimbalRange); //Get new Z rotation and clamp by gimbal range
            pivot.localEulerAngles = Vector3.forward * pivotRotation;                      //Apply rotation to pivot

            //Play SFX:
            if (force != 0)
            {
                if (!GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Play("CannonRotate", gameObject);
            }
            else if (GameManager.Instance.AudioManager.IsPlaying("CannonRotate", gameObject)) GameManager.Instance.AudioManager.Stop("CannonRotate", gameObject);
        }

        //UTILITY METHODS:
        /// <summary>
        /// Changes beam length to given value (in units).
        /// </summary>
        private void SetBeamLength(float newLength)
        {
            Vector3 newScale = beam.localScale;   //Get scale from beam object
            newScale.x = newLength;               //Set new beam length
            beam.transform.localScale = newScale; //Apply new scale
        }
        /// <summary>
        /// Changes beam width to given value (in units)
        /// </summary>
        /// <param name="newWidth"></param>
        private void SetBeamWidth(float newWidth)
        {
            Vector3 newScale = beam.localScale;   //Get scale from beam object
            newScale.y = newWidth;                //Set new beam width
            beam.transform.localScale = newScale; //Apply new scale
        }
    }
}