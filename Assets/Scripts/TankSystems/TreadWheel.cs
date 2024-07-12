using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreadWheel : MonoBehaviour
{
    //Objects & Components:
    private TreadSystem treadSystem;     //The tread system this wheel is linked to (attached to parent object)
    private CircleCollider2D wheelGuard; //Reference to wheel guard collider (if being used)

    //Settings:
    [Header("Properties:")]
    [Tooltip("Maximum upward distance wheel can travel before it hits a hard stop."), Min(0)]                  public float maxSuspensionDepth;
    [Tooltip("Maximum speed at which wheels can spring to target position when uncompressed."), Min(0)]        public float maxSpringSpeed;
    [Tooltip("Maximum speed at which wheels can spring to target position when compressed."), Min(0)]          public float maxSqueezeSpeed;
    [Tooltip("Extra radius around wheel used to maintain ground status when wheel is decompressing."), Min(0)] public float groundDetectBuffer;
    [Space()]
    [Tooltip("How much force wheel suspension exerts to support tank."), Min(0)]               public float stiffness;
    [Tooltip("Curve representing suspension stiffness based on wheel compression amount.")]    public AnimationCurve stiffnessCurve;
    [Tooltip("Force which opposes suspension motion and allows tank to come to a rest.")]      public float damper;
    [Tooltip("Prevents wheel from applying stick force to surfaces (good for extra wheels).")] public bool nonStick;
    [Header("Other Settings:")]
    [Tooltip("Causes wheel to generate a collider which prevents tank from squishing it into the ground once it's reached its compression limit.")] public bool generateWheelGuard = true;
    [Tooltip("Hides debug visualization meshes on wheels.")]                                                                                        public bool hideDebugs;

    //Runtime Variables:
    private Vector2 basePosition;        //Natural position of wheel (set at start)
    internal float radius;               //Radius of wheel, recorded at start
    internal bool grounded;              //True if wheel is touching a surface, false if not
    internal RaycastHit2D lastGroundHit; //Information about last surface hit by wheel

    internal float springSpeed;          //Speed at which wheel is currently moving along spring
    internal float compressionValue;     //Value between 0 - 1 representing how compressed this wheel currently is
    internal float prevCompressionValue; //Compression value from last update

    //RUNTIME METHODS:
    private void Awake()
    {
        //Get objects & components:
        treadSystem = GetComponentInParent<TreadSystem>(); //Get tread system controller from parent

        //Get runtime values:
        basePosition = transform.localPosition; //Get base position of wheel
        radius = transform.localScale.x / 2;    //Get wheel radius

        //Set up wheel guard:
        if (generateWheelGuard) //Wheel needs to generate a guard object
        {
            wheelGuard = Instantiate(new GameObject(), transform.parent).AddComponent<CircleCollider2D>();    //Instantiate an object with a circle collider
            wheelGuard.gameObject.layer = LayerMask.NameToLayer("Treads");                                    //Set collider layer to treads
            wheelGuard.radius = radius;                                                                       //Set collider radius to radius of wheel
            wheelGuard.transform.localPosition = transform.localPosition + (Vector3.up * maxSuspensionDepth); //Position guard at end of suspension stroke
            wheelGuard.gameObject.name = name + "_wheelGuard";                                                //Name object so it's not as confusing to look at in the inspector
        }
    }
    private void Update()
    {
        //Update wheel position:
        Vector2 backstopPos = transform.parent.TransformPoint(basePosition + (Vector2.up * maxSuspensionDepth)); //Get position wheel will move to when most compressed
        Vector2 extendPos = transform.parent.TransformPoint(basePosition);                                       //Get position wheel could be at when most extended
        bool prevGrounded = grounded;                                                                            //Update grounded status
        if (Physics2D.OverlapCircle(backstopPos, radius, LayerMask.GetMask("Ground")) != null) //There is ground intersecting with wheel backstop position
        {
            grounded = true;                  //Indicate that wheel is grounded
            transform.position = backstopPos; //Snap to backstop position
        }
        else //Wheel suspension is not fully compressed
        {
            Vector2 targetPosition = Vector2.MoveTowards(transform.position, extendPos, maxSpringSpeed);                                                            //Get target position wheel can actually move to
            lastGroundHit = Physics2D.CircleCast(backstopPos, radius, -transform.parent.up, (backstopPos - targetPosition).magnitude, LayerMask.GetMask("Ground")); //Look for ground within area wheel will be touching
            if (lastGroundHit.collider != null) //Wheel can hit ground
            {
                grounded = true;                                                                               //Indicate that wheel is grounded
                targetPosition = backstopPos + (Vector2)(-transform.parent.up * lastGroundHit.distance);       //Get position that would put wheel exactly on the ground
                transform.position = Vector2.MoveTowards(transform.position, targetPosition, maxSqueezeSpeed); //Use squeeze speed to move wheel toward grounded position
            }
            else //Wheel is unobstructed
            {
                if (Physics2D.OverlapCircle(targetPosition, radius + groundDetectBuffer, LayerMask.GetMask("Ground")) != null) grounded = true; //Wheel is lifting but is still kind of on the ground
                else grounded = false; //Indicate that wheel is not grounded
                transform.position = targetPosition; //Move wheel toward fully extended position
            }
        }

        //Get compression value:
        prevCompressionValue = compressionValue;                                                              //Store previous compression value
        if (!grounded) compressionValue = 0;                                                                  //No force is exerted on tank by wheels which are not grounded
        else compressionValue = 1 - (Vector2.Distance(backstopPos, transform.position) / maxSuspensionDepth); //Use distance from backstop to determine how compressed wheel is
        springSpeed = (compressionValue - prevCompressionValue) / Time.deltaTime;                             //Get current speed at which suspension is moving (while compressed)

        //Testing updates:
        if (Application.isEditor) wheelGuard.transform.localPosition = (Vector3)basePosition + (Vector3.up * maxSuspensionDepth); //Update guard position in case the suspension depth setting has been tweaked
    }
    private void OnDrawGizmos()
    {
        if (!hideDebugs) //Debugs are not currently hidden
        {
            //Draw wheel properties:
            Gizmos.color = Color.Lerp(Color.green, Color.red, compressionValue);                        //Use compression value to determine wheel color
            Gizmos.DrawWireSphere(transform.position, transform.localScale.x / 2);                      //Draw wheel outline
            Gizmos.DrawWireSphere(transform.position, transform.localScale.x / 2 + groundDetectBuffer); //Draw ground buffer area

            Gizmos.color = Color.cyan;                                                                                                  //Always make suspension lines cyan
            Vector2 lineOrigin = (Application.isPlaying ? (Vector2)transform.parent.TransformPoint(basePosition) : transform.position); //Get position for line to start at depending on whether or not basePosition is initialized
            Vector2 lineTarget = lineOrigin + ((Vector2)transform.parent.up * maxSuspensionDepth);                                      //Get target position for line based on system orientation and lift depth
            Gizmos.DrawLine(lineOrigin, lineTarget);                                                                                    //Draw line representing suspension depth
        }
    }
}
