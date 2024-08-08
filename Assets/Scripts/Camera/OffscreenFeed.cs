using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine.UI;

public class OffscreenFeed : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Unique follower camera for this feed.")]                     private Camera cam;
    [Tooltip("Texture camera renders to.")]                                private RenderTexture targetTex;
    [Tooltip("UI object containing visualization bubble.")]                private RectTransform bubbleObject;
    [Tooltip("Arrow on bubble indicating direction of offscreen player.")] private RectTransform bubblePoint;

    //Settings:
    [Header("Camera Settings:")]
    [SerializeField, Tooltip("Resolution of output texture."), Min(1)]                                       private int renderResolution = 100;
    [SerializeField, Tooltip("Sets orthographic size of camera."), Min(0)]                                   private float baseOrthoSize;
    [SerializeField, Tooltip("Default radial size of display bubble."), Min(0.01f)]                          private float baseBubbleRadius;
    [SerializeField, Tooltip("Distance from edge of screen bubble is placed at.")]                           private float bubbleDistFromEdge;
    [SerializeField, Tooltip("Curve describing animation when bubble appears or disappears.")]               private AnimationCurve transitionCurve;
    [SerializeField, Tooltip("How much time it takes for bubble to transition between states."), Min(0.01f)] private float transitionTime = 0.01f;
    [SerializeField, Tooltip("This enables functionality for display bubble to change size depending on distance from screen bounds.")] private bool useDynamicBubbleSize;
    [ShowIf("useDynamicBubbleSize"), SerializeField, Tooltip("Smallest size display bubble can get (largest is baseBubbleSize).")]      private float minBubbleRadius;
    [ShowIf("useDynamicBubbleSize"), SerializeField, Tooltip("Distance from screen bounds at which bubble disappears.")]                private float maxVisDistance;

    //Runtime Variables:
    private bool prevEnabled; //Whether or not visualization bubble was enabled last frame
    private float transTime;  //Time since last transition

    //UNITY METHODS:
    private void Awake()
    {
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
        //Bubble updates:
        Vector2 viewportPos = Camera.main.WorldToViewportPoint(transform.position); //Get position of object relative to camera viewport
        bool bubbleActive = viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1; //Get value indicating whether or not bubble should be active
        if (bubbleActive || transTime > 0) //Bubble is being actively updated
        {
            //Check for initial activation:
            if (bubbleActive && !prevEnabled) //Object has just left camera view
            {
                cam.gameObject.SetActive(true);          //Activate camera
                bubbleObject.gameObject.SetActive(true); //Activate visualization bubble
                prevEnabled = true;                      //Indicate that visualization is now enabled
            }

            //Handle transition:
            if (bubbleActive && transTime != transitionTime) transTime = Mathf.Min(transTime + Time.deltaTime, transitionTime); //Increment transition timer, capping out at max transition time
            else if (!bubbleActive && transTime != 0) transTime = Mathf.Max(transTime - Time.deltaTime, 0);                     //Decrement transition timer so that animation plays in reverse
            float interpolant = transitionCurve.Evaluate(transTime / transitionTime);                                           //Get interpolant value to animate bubble movement

            //Update bubble direction:
            Vector2 colliderPoint = Camera.main.GetComponentInChildren<Collider2D>().ClosestPoint(transform.position); //Get point on camera collider closest to current position of object
            Vector2 normal = ((Vector2)transform.position - colliderPoint).normalized;                                 //Get direction between point on collider and object position
            bubblePoint.localEulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.up, normal);                  //Rotate bubble point to align with side of camera collider
            cam.gameObject.transform.localEulerAngles = Vector3.zero;                                                  //Make sure camera rotation is zeroed out

            //Update bubble position:
            Vector2 worldPos = colliderPoint + (-normal * (bubbleDistFromEdge + (baseBubbleRadius / 2))); //Get world (relative to camera) position of bubble point
            Vector2 targetPos = Camera.main.WorldToScreenPoint(worldPos);                                 //Get bubble's target position (in UI space) relative to edge of camera view
            bubbleObject.position = targetPos;

            //Update bubble scale:
            bubbleObject.localScale = interpolant * (baseBubbleRadius / 2) * Vector3.one; //Set bubble size (animate using interpolant value)

            //Check for deactivation:
            if (!bubbleActive && prevEnabled && transTime == 0) //Object has left camera view and bubble has finished animation
            {
                cam.gameObject.SetActive(false);          //Deactivate camera
                bubbleObject.gameObject.SetActive(false); //Deactivate visualization bubble
                prevEnabled = false;                      //Indicate that visualization is now disabled
            }
        }
    }
}
