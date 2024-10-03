using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleTankBrain : MonoBehaviour
{
    public enum TankBehavior { 
        PATROL, //Moving back and forth in a set pattern
        PURSUE, //Detects the player - moving into range
        ENGAGE, //In optimal range, tries to maintain distance & shoots cannons
        FLEE, //Trying to run away
        SURRENDER //Throws Up White Flag
    }
    public TankBehavior state;
    private TankController tank;
    private TankManager tankMan;
    private GunController[] guns;
    public Transform flag;

    //Ai Variables
    [SerializeField, Tooltip("Time in seconds between each decision this tank makes")] public float decisionCooldown;
    private float decisionTimer;
    private GameObject playerTank;
    private Transform playerTankTransform;
    [SerializeField, Tooltip("How far away the player tank is from this tank")] public float distanceToPlayer;

    [SerializeField, Tooltip("How far away the enemy can detect the player from")] public float maxDetectionRange;
    [SerializeField, Tooltip("How far away the enemy wants to engage the player from")] public float maxEngagementRange;
    [SerializeField, Tooltip("Wants to stay beyond this threshold when engaging the player")] public float minEngagementRange;

    //Other 
    private float engineCooldownTimer = 0;

    public void Awake()
    {
        tank = GetComponent<TankController>();
        tankMan = GameObject.Find("TankManager").GetComponent<TankManager>();

        state = TankBehavior.PATROL;

        GetPlayerTank();
        decisionTimer = decisionCooldown;
    }

    private void GetPlayerTank()
    {
        foreach(TankId tank in tankMan.tanks)
        {
            if (tank.tankType == TankId.TankType.PLAYER)
            {
                playerTank = tank.gameObject;
                playerTankTransform = tank.gameObject.GetComponentInChildren<TreadSystem>().transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        decisionTimer -= Time.deltaTime;
        if (playerTankTransform != null) distanceToPlayer = tank.treadSystem.transform.position.x - playerTankTransform.position.x;

        //Check to see if we should flee
        if (decisionTimer <= 0 && CheckGuns() == false)
        {
            /*
            if (state != TankBehavior.FLEE)
            {
                state = TankBehavior.FLEE;
                Debug.Log("Ah shit");
            }*/

            if (state != TankBehavior.SURRENDER)
            {
                if (tank.treadSystem.currentEngines <= 0)
                {
                    flag.gameObject.SetActive(true);
                    Debug.Log("You have bested me.");
                    state = TankBehavior.SURRENDER;
                }
            }
        }

        switch (state)
        {
            case TankBehavior.PATROL: Patrol(); break;
            case TankBehavior.PURSUE: Pursue(); break;
            case TankBehavior.ENGAGE: Engage(); break;
            case TankBehavior.FLEE: Flee(); break;
            case TankBehavior.SURRENDER: Surrender(); break;
        }

        engineCooldownTimer -= Time.deltaTime;
        if (engineCooldownTimer <= 0)
        {
            engineCooldownTimer = 12f;
            LoadAllEngines(15);
        }
    }

    public void Patrol()
    {
        if (decisionTimer <= 0)
        {
            //Stay in place, occasionally moving left and right
            if (tank.treadSystem.gear > 0)
            {
                tank.ShiftLeft();
                decisionTimer = decisionCooldown * 0.5f;
            }
            else if (tank.treadSystem.gear < 0)
            {
                tank.ShiftRight();
                decisionTimer = decisionCooldown * 0.5f;
            }
            else
            {
                float random = Random.Range(2f, 5f);
                StartCoroutine(MoveShort(random));

                decisionTimer = decisionCooldown + random;
            }
        }
    }

    public IEnumerator MoveShort(float time)
    {
        int random = Random.Range(0, 2);
        if (random == 0) tank.ShiftLeft();
        if (random == 1) tank.ShiftRight();
        yield return new WaitForSeconds(time);
        if (random == 0) tank.ShiftRight();
        if (random == 1) tank.ShiftLeft();

        //Found the player
        if (distanceToPlayer <= maxDetectionRange)
        {
            decisionTimer = 0.5f;
            state = TankBehavior.PURSUE;
        }
    }

    public void Pursue()
    {
        if (decisionTimer <= 0)
        {
            //Chase the player
            if (distanceToPlayer <= maxEngagementRange)
            {
                state = TankBehavior.ENGAGE;
                Debug.Log("PREPARE TO GET FUCKED BY ME, " + tank.TankName + "!");
                tank.EnableCannonBrains(true);
            }
            else
            {
                if (tank.treadSystem.gear > -2)
                {
                    tank.ShiftLeft();
                }
            }

            //Player is out of range
            if (distanceToPlayer >= maxDetectionRange)
            {
                Debug.Log("Damn, I lost em");
                tank.ShiftRight();
                state = TankBehavior.PATROL;
            }

            decisionTimer = decisionCooldown * 0.5f;
        }
    }

    public void Engage()
    {
        if (decisionTimer <= 0)
        {
            if (tank.treadSystem.gear < 0) //Slowdown
            {
                tank.ShiftRight();
            }

            //Maintain Distance
            if (distanceToPlayer <= minEngagementRange)
            {
                tank.ShiftRight();
                Debug.Log("Woa woa take it easy");
            }
            else if (distanceToPlayer <= maxEngagementRange)
            {
                if (tank.treadSystem.gear > 0)
                {
                    tank.ShiftLeft();
                }
            }
            decisionTimer = decisionCooldown;
            
            //Player moves outside of engagement range
            if (distanceToPlayer > maxEngagementRange)
            {
                tank.EnableCannonBrains(false);
                if (distanceToPlayer <= maxDetectionRange)
                {
                    decisionTimer = 0.5f;
                    state = TankBehavior.PURSUE;
                    Debug.Log("Get back here!");
                }
                else
                {
                    decisionTimer = decisionCooldown * 0.5f;
                    state = TankBehavior.PATROL;
                }
            }
        }
    }

    public void Flee()
    {
        if (decisionTimer <= 0)
        {
            if (tank.treadSystem.gear < 2)
            {
                Debug.Log("Zoinks! I'm gettin outa here!");
                tank.ShiftRight();
            }

            decisionTimer = 0.5f;
        }
    }

    public void Surrender()
    {
        if (decisionTimer <= 0)
        {
            if (tank.treadSystem.gear > 0)
            {
                tank.ShiftLeft();
            }

            if (tank.treadSystem.gear < 0)
            {
                tank.ShiftRight();
            }

            decisionTimer = 0.5f;
        }
    }

    public bool CheckGuns()
    {
        guns = tank.GetComponentsInChildren<GunController>();
        foreach (GunController gun in guns)
        {
            if (gun != null) return true;
        }
        return false;
    }

    public void LoadAllEngines(int amount)
    {
        EngineController[] engines = GetComponentsInChildren<EngineController>();
        foreach(EngineController engine in engines)
        {
            engine.AddPressure(amount, false);
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (tank != null)
        {
            //Detection Range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tank.treadSystem.transform.position, maxDetectionRange);

            //Engagement Range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tank.treadSystem.transform.position, maxEngagementRange);
            Gizmos.DrawWireSphere(tank.treadSystem.transform.position, minEngagementRange);
        }
    }
}
