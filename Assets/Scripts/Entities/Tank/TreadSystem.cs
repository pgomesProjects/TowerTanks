using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreadSystem : MonoBehaviour
{
    //Objects & Components:
    [Tooltip("Controller for this tread's parent tank.")]                                                         private TankController tankController;
    [Tooltip("Rigidbody for affecting tank movement.")]                                                           internal Rigidbody2D r;
    [Tooltip("Wheels controlled by this system.")]                                                                private TreadWheel[] wheels;
    [SerializeField, Tooltip("Prefab which will be used to generate caterpillar treads (should be 1 unit long)")] private GameObject treadPrefab;
    [Tooltip("Array of all tread segments in system (one per wheel).")]                                           private Transform[] treads;

    //Settings:
    [Header("Center of Gravity Settings:")]
    [Tooltip("Height at which center of gravity is locked relative to tread system.")] public float COGHeight;
    [Tooltip("Extents of center of gravity (affects how far tank can lean).")]         public float COGWidth;
    [Tooltip("How much weight the tank currently has")]                                public float totalWeight = 0;

    [Header("Drive Settings:")]
    [Tooltip("Greatest speed tank can achieve at maximum gear.")]                                              public float maxSpeed = 100;
    [SerializeField, Tooltip("Rate at which tank accelerates to target speed (in units per second squared).")] private float maxAcceleration;
    [Range(0, 1), SerializeField, Tooltip("Lerp value used to smooth out end of acceleration phases.")]        private float accelerationDamping = 0.5f;
    [Min(1), Tooltip("Number of positions throttle can be in (includes neutral and reverse)")]                 public int gearPositions = 5;
    [Space()]
    [SerializeField, Min(0), Tooltip("Force which tries to keep wheels stuck to the ground.")]                      private float wheelStickiness;
    [SerializeField, Tooltip("Curve describing how sticky wheel is based on compression value.")]                   private AnimationCurve wheelStickCompressionCurve;
    [Min(0), SerializeField, Tooltip("Number of seconds to wait while in neutral before activating parking brake")] private float parkingBrakeWait;
    [SerializeField, Tooltip("Stopping power of parking brake.")]                                                   private float parkingBrakeStrength;

    [Header("Traction & Drag Settings:")]
    [SerializeField, Tooltip("Default drag factor applied by air causing tank to lean while in motion (scales based on speed)."), Min(0)] private float baseAirDragForce;
    [SerializeField, Tooltip("Angular drag when all (non-extra) wheels are on the ground."), Min(0)]                                      private float maxAngularDrag;
    [SerializeField, Tooltip("How many wheels are by default off the ground."), Min(0)]                                                   private int extraWheels;
    [Space()]
    [Range(0, 90), SerializeField, Tooltip("Maximum angle (left or right) at which tank can be tipped.")]                                             private float maxTipAngle;
    [Min(0), SerializeField, Tooltip("Radial area (inside max tip angle) where torque will be applied to prevent tank from reaching max tip angle.")] private float tipAngleBufferZone;
    [Min(0), SerializeField, Tooltip("Scale of force applied to prevent tippage.")]                                                                   private float tipPreventionForce;

    //Runtime Variables:
    private bool initialized;    //True if tread system has already been set up
    internal int currentEngines; //Current number of engines acting on tread system (deprecated?)
    internal int gear;           //Basic designator for current direction and speed the tank is set to move in (0 = Neutral)
    private float throttleValue; //Current value of the throttle, adjusted over time based on acceleration and gear for smooth movement
    private float timeInGear;    //Time (in seconds) treads have spent in current gear

    //RUNTIME METHODS:
    private void Awake()
    {
        Initialize(); //Set up treads
    }
    private void Update()
    {
        //Update timers:
        timeInGear += Time.deltaTime; //Update time in gear tracker

        //Update treads:
        for (int wheelIndex = 0; wheelIndex < wheels.Length; wheelIndex++) //Iterate once for each wheel
        {
            int nextWheelIndex = wheelIndex + 1; if (nextWheelIndex == wheels.Length) nextWheelIndex = 0; //Get index for wheel after current one (wrap around at last wheel)
            Transform wheel1 = wheels[wheelIndex].transform;                                              //Get transform from first wheel
            Transform wheel2 = wheels[nextWheelIndex].transform;                                          //Get transform from second wheel
            float treadWidth = Vector2.Distance(wheel1.position, wheel2.position);                        //Tread width is the exact distance between both wheels
            Vector2 treadPos = Vector2.Lerp(wheel1.position, wheel2.position, 0.5f);                      //Position of tread starts exactly between both wheels
            Vector2 treadNormal = Vector2.Perpendicular(wheel1.position - wheel2.position).normalized;    //Get normal of tread so that it can be moved later (using difference in position between both wheels)
            treadPos += treadNormal * wheels[wheelIndex].radius;                                          //Move tread position to account for radius of wheel

            Transform tread = treads[wheelIndex];                                               //Get tread at current wheel index
            tread.position = treadPos;                                                          //Move tread to target position
            tread.eulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.up, treadNormal); //Rotate tread to target rotation
            tread.localScale = new Vector3(treadWidth, tread.localScale.y, 1);                  //Scale tread to target length
        }
    }
    private void FixedUpdate()
    {
        //Update throttle:
        float throttleTarget = gear / (float)((gearPositions - 1) / 2);                                          //Get target throttle value between -1 and 1 based on current gear setting
        throttleTarget = Mathf.Lerp(throttleValue, throttleTarget, accelerationDamping);                         //Use lerp to soften throttle target, making accelerations less abrupt
        throttleValue = Mathf.MoveTowards(throttleValue, throttleTarget, maxAcceleration * Time.fixedDeltaTime); //Move throttle value to designated target without exceeding given max acceleration

        //Count grounded wheels
        int groundedWheels = 0;                                                         //Initialize variable to track how many wheels are grounded
        foreach (TreadWheel wheel in wheels) { if (wheel.grounded) groundedWheels++; }; //Pre-calculate number of grounded wheels
        
        //Apply wheel forces:
        Vector2 targetTankSpeed = transform.right * maxSpeed * throttleValue;                  //Get target speed based on tank throttle
        Vector2 deltaSpeed = targetTankSpeed - r.velocity;                                     //Get value which would change current speed to target speed
        Vector2 baseWheelAccel = deltaSpeed / Time.fixedDeltaTime;                             //Get ideal acceleration value which each wheel will use to compute actual force (apply actual acceleration to smooth out speed changes)
        baseWheelAccel *= Mathf.Min((float)groundedWheels / (wheels.Length - extraWheels), 1); //Handicap acceleration when wheels are off ground (prevents tank from doing extended wheelies)
        foreach (TreadWheel wheel in wheels) //Iterate through wheel list
        {
            if (wheel.grounded) //Only apply force from grounded wheels
            {
                //Get suspension force:
                float suspensionMagnitude = wheel.stiffnessCurve.Evaluate(wheel.compressionValue) * wheel.stiffness; //Use wheel compression value and stiffness to determine magnitude of exerted force
                float dragMagnitude = wheel.damper * wheel.springSpeed;                                              //Get magnitude of force applied by spring damper (drag and inefficiency of suspension)
                Vector2 suspensionForce = transform.up * (suspensionMagnitude + dragMagnitude);                      //Get directional force to apply to rigidbody
                r.AddForceAtPosition(suspensionForce, wheel.transform.position, ForceMode2D.Force);                  //Apply total spring forces to rigidbody at position of wheel

                //Terrain interaction behaviors:
                Vector2 wheelDirection = Vector2.Perpendicular(wheel.lastGroundHit.normal); //Get direction wheel is applying force in
                if (wheel.lastGroundHit.collider != null) //Wheel has valid information about hit ground
                {
                    //Apply drive torque:
                    Vector2 wheelAccel = Vector3.Project(baseWheelAccel, wheelDirection); //Project base acceleration onto vector representing direction wheel is capable of producing force in (depends on ground angle)
                    wheelAccel /= (wheels.Length - extraWheels);                          //Divide wheel acceleration value by number of main wheels so that tank is most stable when all wheels are on the ground
                    Debug.DrawRay(wheel.lastGroundHit.point, wheelAccel);
                    r.AddForceAtPosition(wheelAccel, wheel.lastGroundHit.point, ForceMode2D.Force); //Apply wheel traction to system

                    //Apply wheel stickiness:
                    if (!wheel.nonStick && wheel.springSpeed < 0) //Wheel appears to be leaving the ground (negative spring speed indicates that spring is decompressing)
                    {
                        float stickForce = wheelStickiness * -wheel.springSpeed * (1 - wheel.compressionValue);                       //Determine stick force based on setting and velocity of wheel decompression
                        stickForce *= wheelStickCompressionCurve.Evaluate(wheel.compressionValue);                                    //Modify stick force based on how compressed wheel is (prevents nasty behavior which artificially compresses tank)
                        r.AddForceAtPosition(-wheel.lastGroundHit.normal * stickForce, wheel.lastGroundHit.point, ForceMode2D.Force); //Apply downward stick force on tank at position of wheel (based on direction of wheel contact with ground)
                        Debug.DrawRay(wheel.lastGroundHit.point, -transform.up * stickForce, Color.yellow);
                    }
                }
            }
        }

        //Add air drag:
        float actualAirDrag = baseAirDragForce * Time.fixedDeltaTime * r.velocity.x; //Calculate air drag based on given value and horizontal speed of tank
        r.AddTorque(actualAirDrag, ForceMode2D.Force);                               //Apply force as torque to tread system rigidbody (tilting it away from direction of motion)
        
        //Add angular drag:
        groundedWheels = Mathf.Min(groundedWheels, wheels.Length - extraWheels);              //Cap grounded wheels in case extras would push number over calculated maximum
        r.angularDrag = maxAngularDrag * Mathf.Min((float)groundedWheels / wheels.Length, 1); //Make angular drag proportional to number of grounded (non-extra) wheels

        //Prevent tipping:
        if (r.rotation > maxTipAngle - tipAngleBufferZone || r.rotation < (maxTipAngle - tipAngleBufferZone)) //Tank is getting close to tipping over
        {
            if (r.rotation > maxTipAngle || r.rotation < -maxTipAngle) //Tank is exceeding absolute max tip angle
            {
                r.MoveRotation(maxTipAngle * Mathf.Sign(r.rotation)); //Hard cap tip angle
                r.angularVelocity = 0;                                //Negate angular velocity
            }
            float correctiveForce = Mathf.InverseLerp(maxTipAngle - tipAngleBufferZone, maxTipAngle, Mathf.Abs(r.rotation)); //Get value between 0 and 1 representing how close tank is to tipping over
            correctiveForce *= tipPreventionForce * -Mathf.Sign(r.rotation);                                                 //Calculate corrective force based on setting, angular position in buffer zone, and direction of tippage
            r.AddTorque(correctiveForce, ForceMode2D.Force);                                                                 //Apply corrective force to prevent tippage
        }
        
    }
    private void OnDrawGizmos()
    {
        //Draw center of mass:
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.TransformPoint(new Vector2(-COGWidth / 2, COGHeight)), transform.TransformPoint(new Vector2(COGWidth / 2, COGHeight)));
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetComponent<Rigidbody2D>().worldCenterOfMass, 0.2f);

        //Draw tip prevetion diagram:
        Gizmos.DrawRay(transform.position, transform.up * 3);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(maxTipAngle, Vector3.forward) * Vector3.up * 3);
        Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(-maxTipAngle, Vector3.forward) * Vector3.up * 3);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(maxTipAngle - tipAngleBufferZone, Vector3.forward) * Vector3.up * 3);
        Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(-(maxTipAngle - tipAngleBufferZone), Vector3.forward) * Vector3.up * 3);
    }

    //FUNCTIONALITY METHODS:
    public void Initialize()
    {
        //Initialization check:
        if (initialized) return; //Do not re-initialize treads
        initialized = true;      //Indicate that treads have been initialized

        //Get objects & components:
        tankController = GetComponentInParent<TankController>(); //Get tank controller object from parent
        r = GetComponent<Rigidbody2D>();                         //Get rigidbody component
        wheels = GetComponentsInChildren<TreadWheel>();          //Get array of all wheels in system

        //Generate treads:
        List<Transform> newTreads = new List<Transform>(); //Instantiate list to store spawned treads
        for (int x = 0; x < wheels.Length; x++) //Iterate once for each wheel in tank
        {
            Transform newTread = Instantiate(treadPrefab, transform).transform; //Instantiate new tread object
            newTreads.Add(newTread);                                            //Add new tread to list
        }
        treads = newTreads.ToArray(); //Commit generated list to array
    }

    //UTILITY METHODS:
    /// <summary>
    /// Evaluates mass and center of gravity for tank depending on position and quantity of cells.
    /// </summary>
    public void ReCalculateMass()
    {
        //Initialization:
        int cellCount = 0;                  //Initialize value to store number of cells counted by evaluation
        Vector2 avgCellPos = new Vector2(); //Initialize value to store average cell position

        //Cell roundup:
        foreach (Room room in tankController.rooms) //Iterate through each room in tank
        {
            foreach (Cell cell in room.cells) //Iterate through each cell in room
            {
                cellCount++;                                                                     //Add to cell count
                avgCellPos += (Vector2)transform.InverseTransformPoint(cell.transform.position); //Add local position of cell (relative to tank) to average
            }
        }

        //Calculation:
        totalWeight = cellCount * 50f;
        avgCellPos /= cellCount;                                                                         //Get average position of cells
        //r.centerOfMass = new Vector2(Mathf.Clamp(avgCellPos.x, -COGWidth / 2, COGWidth / 2), COGHeight); //Constrain center mass to line segment controlled in settings (for tank handling reliability)
    }
    /// <summary>
    /// Shifts to target gear.
    /// </summary>
    /// <param name="targetGear"></param>
    public void ChangeGear(int targetGear)
    {
        gear = -targetGear; //Update gear setting
        timeInGear = 0;     //Reset time in gear counter
    }
}