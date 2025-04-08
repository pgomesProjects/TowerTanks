using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class OffscreenFeed : MonoBehaviour
    {
        //Objects & Components:
        [Tooltip("Unique follower camera for this feed.")] private Camera cam;
        [Tooltip("Texture camera renders to.")] private RenderTexture targetTex;
        [Tooltip("UI object containing visualization bubble.")] private RectTransform bubbleObject;
        [Tooltip("Arrow on bubble indicating direction of offscreen player.")] private RectTransform bubblePoint;

        //Settings:
        [Header("Camera Settings:")]
        [SerializeField, Tooltip("Resolution of output texture."), Min(1)] private int renderResolution = 100;
        [SerializeField, Tooltip("Sets orthographic size of camera."), Min(0)] private float baseOrthoSize;
        [SerializeField, Tooltip("Default radial size of display bubble."), Min(0.01f)] private float baseBubbleRadius;
        [SerializeField, Tooltip("Distance from edge of screen bubble is placed at.")] private float bubbleDistFromEdge;
        [SerializeField, Tooltip("Curve describing animation when bubble appears or disappears.")] private AnimationCurve transitionCurve;
        [SerializeField, Tooltip("How much time it takes for bubble to transition between states."), Min(0.01f)] private float transitionTime = 0.01f;
        [SerializeField, Tooltip("Starting point of animation where bubble slides onscreen (should be based on bubbleDistFromEdge.")] private float transitionSlideDist;
        [SerializeField, Tooltip("If true, bubble size will stay consistent (in world) when camera size changes.")] private bool scaleBubbleWithCamera;
        [SerializeField, Tooltip("If true, camera zoom will stay consistent when main camera size changes.")] private bool scaleOrthoWithCamera;
        [SerializeField, Tooltip("This enables functionality for display bubble to change size depending on distance from screen bounds.")] private bool useDynamicBubbleSize;
        [ShowIf("useDynamicBubbleSize"), SerializeField, Tooltip("Smallest size display bubble can get (largest is baseBubbleSize).")] private float minBubbleRadius;
        [ShowIf("useDynamicBubbleSize"), SerializeField, Tooltip("Distance from screen bounds at which bubble disappears.")] private float maxVisDistance;

        //Runtime Variables:
        private bool prevEnabled;           //Whether or not visualization bubble was enabled last frame
        private float transTime;            //Time since last transition
        private Vector2 lastPointNormal;    //Latest direction of bubble point
        private Vector2 lastWorldTargetPos; //Latest world position of bubble

        //UNITY METHODS:
        private void Awake()
        {
            if (GameHUD.main == null)
            {
                Debug.LogWarning("No GameHUD found!");
                Destroy(this);
            }

            //Generate camera object:
            cam = new GameObject().AddComponent<Camera>();   //Create object with attached camera component
            cam.transform.parent = transform;                //Child camera to object this script is on
            cam.transform.localPosition = Vector3.back * 10; //Move camera to be on top of object and in front of it
            cam.orthographic = true;                         //Set camera to orthographic
            cam.orthographicSize = baseOrthoSize;            //Set orthographic size of camera
        }
        private void Start()
        {
            //Generate bubble object:
            targetTex = new RenderTexture(renderResolution, renderResolution, 24);                                                        //Create texture for camera to render to
            cam.targetTexture = targetTex;                                                                                                //Have camera render to texture
            bubbleObject = Instantiate(Resources.Load<GameObject>("UI/VisBubble"), GameHUD.main.transform).GetComponent<RectTransform>(); //Generate visualizer in hud
            bubbleObject.GetComponentInChildren<RawImage>().texture = targetTex;                                                          //Set texture in UI to camera output texture
            bubblePoint = bubbleObject.GetChild(0).GetComponent<RectTransform>();                                                         //Get transform for point indicating directionality of bubble

            cam.gameObject.SetActive(false);          //Disable camera until relevant
            bubbleObject.gameObject.SetActive(false); //Disable bubble until relevant
        }
        private void Update()
        {
            //NOTE: FIX TO WORK WITH CAMMANIPULATOR AT SOME POINT
            return;

            //Bubble updates:
            Vector2 viewportPos = Camera.main.WorldToViewportPoint(transform.position); //Get position of object relative to camera viewport
            if (viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1) //Object is outside camera viewport
            {
                if (!prevEnabled) //Object has just left camera view
                {
                    cam.gameObject.SetActive(true);          //Activate camera
                    bubbleObject.gameObject.SetActive(true); //Activate visualization bubble
                    prevEnabled = true;                      //Indicate that visualization is now enabled
                }

                //Handle transition:
                if (transTime != transitionTime) transTime = Mathf.Min(transTime + Time.deltaTime, transitionTime); //Increment transition timer, capping out at max transition time
                float interpolant = transitionCurve.Evaluate(transTime / transitionTime);                           //Get interpolant value to animate bubble movement

                //Update bubble direction:
                Vector2 colliderPoint = Camera.main.GetComponentInChildren<Collider2D>().ClosestPoint(transform.position); //Get point on camera collider closest to current position of object
                lastPointNormal = ((Vector2)transform.position - colliderPoint).normalized;                                //Get direction between point on collider and object position
                bubblePoint.localEulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.up, lastPointNormal);         //Rotate bubble point to align with side of camera collider
                cam.gameObject.transform.localEulerAngles = Vector3.zero;                                                  //Make sure camera rotation is zeroed out

                //Update bubble position:
                lastWorldTargetPos = colliderPoint + (-lastPointNormal * (bubbleDistFromEdge + (baseBubbleRadius / 2)));          //Get world (relative to camera) position of target bubble point
                Vector2 targetPos = Camera.main.WorldToScreenPoint(lastWorldTargetPos);                                           //Get bubble's target position (in UI space) relative to edge of camera view
                Vector2 originPos = Camera.main.WorldToScreenPoint(lastWorldTargetPos + (lastPointNormal * transitionSlideDist)); //Get starting position of slide animation
                bubbleObject.position = Vector2.LerpUnclamped(originPos, targetPos, interpolant);                                 //Animate bubble towards (or away from) target position

                //Update bubble scale:
                Vector3 newScale = interpolant * (baseBubbleRadius / 2) * Vector3.one;   //Get base scale value based on animation and settings
                if (scaleBubbleWithCamera) newScale /= Camera.main.orthographicSize / 5; //Reference camera scale if using this type of dynamic scaling
                bubbleObject.localScale = newScale;                                      //Set bubble size (animate using interpolant value)

                //Update camera:
                cam.orthographicSize = baseOrthoSize * (scaleOrthoWithCamera ? Camera.main.orthographicSize / 5 : 1); //Set orthographic size of camera (may be based on main camera size)
            }
            else if (prevEnabled) //Object has just entered camera view
            {
                //Handle transition:
                transTime = Mathf.Max(transTime - Time.deltaTime, 0);                                                 //Decrement transition timer so that animation plays in reverse
                float interpolant = transitionCurve.Evaluate(transTime / transitionTime);                             //Get interpolant value from animation curve
                Vector3 newScale = interpolant * (baseBubbleRadius / 2) * Vector3.one;                                //Get base scale value based on animation and settings
                if (scaleBubbleWithCamera) newScale /= Camera.main.orthographicSize / 5;                              //Reference camera scale if using this type of dynamic scaling
                bubbleObject.localScale = newScale;                                                                   //Set bubble size
                Vector2 targetPos = Camera.main.WorldToScreenPoint(lastWorldTargetPos);                               //Get target position for this animation based on data saved from before object entered camera view
                Vector2 originPos = Camera.main.WorldToScreenPoint(transform.position);                               //Have bubble zoom into target as it shrinks
                bubbleObject.position = Vector2.LerpUnclamped(originPos, targetPos, interpolant);                     //Animate bubble
                cam.orthographicSize = baseOrthoSize * (scaleOrthoWithCamera ? Camera.main.orthographicSize / 5 : 1); //Set orthographic size of camera (may be based on main camera size)

                //Hide bubble:
                if (transTime == 0) //Transition has finished
                {
                    cam.gameObject.SetActive(false);          //Deactivate camera
                    bubbleObject.gameObject.SetActive(false); //Deactivate visualization bubble
                    prevEnabled = false;                      //Indicate that visualization is now disabled
                }
            }
        }
    }
}
