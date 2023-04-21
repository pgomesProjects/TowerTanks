using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CameraEventController : MonoBehaviour
{
    public static CameraEventController instance;

    [SerializeField, Tooltip("The camera for in-game events.")] private CinemachineVirtualCamera _gameCamera;
    [SerializeField, Tooltip("The camera for cinematic transitions.")] private CinemachineVirtualCamera _cinematicCamera;
    [SerializeField, Tooltip("The camera for freezing in place.")] private CinemachineVirtualCamera _freezeCamera;

    [SerializeField, Tooltip("The target group for the game camera to follow.")] private CinemachineTargetGroup gameTargetGroup;
    [SerializeField, Tooltip("The target group for the cinematic camera to follow.")] private CinemachineTargetGroup cinematicTargetGroup;
    [SerializeField, Tooltip("The global UI used for certain camera events.")] private GameObject globalUI;


    private CinemachineVirtualCamera _currentActiveCamera;

    //Screenshake variables
    private float shakeTimer, shakeTimerTotal, startingCamIntensity;

    //The blend times for each camera transition
    private float gameToCinematicBlendSeconds = 4;
    private float cinematicToGameBlendSeconds = 2;

    //The list of cameras in the game scene
    private List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

    private void Awake()
    {
        instance = this;

        //Adds each camera to the list of cameras.
        cameras.Add(_gameCamera);
        cameras.Add(_cinematicCamera);
        cameras.Add(_freezeCamera);
    }

    private void Start()
    {
        _currentActiveCamera = _gameCamera;
    }

    // Update is called once per frame
    void Update()
    {
        //Camera shake timer
        if(shakeTimer > 0)
        {
            //Debug.Log("Shake Timer: " + shakeTimer);
            shakeTimer -= Time.deltaTime;

            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(startingCamIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
        }
        else
            _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
    }

    /// <summary>
    /// Enables the target group on the cinematic camera.
    /// </summary>
    /// <param name="newTargetGroup">The target group for the cinematic camera.</param>
    public void EnableCinematicTargetGroup(CinemachineTargetGroup newTargetGroup)
    {
        if (newTargetGroup != null)
            _cinematicCamera.m_Follow = newTargetGroup.transform;
    }

    /// <summary>
    /// Enables the target group on the freeze camera.
    /// </summary>
    /// <param name="newTargetGroup">The target group for the freeze camera.</param>
    public void EnableFreezeTargetGroup(CinemachineTargetGroup newTargetGroup)
    {
        if (newTargetGroup != null)
            _freezeCamera.m_Follow = newTargetGroup.transform;
    }

    /// <summary>
    /// Removes the target group on the freeze camera so that it stays still.
    /// </summary>
    public void DisableFreezeTargetGroup() => _freezeCamera.m_Follow = null;

    /// <summary>
    /// Shakes the current active camera.
    /// </summary>
    /// <param name="intensity">The amplitude of the screenshake.</param>
    /// <param name="seconds">The duration of the screenshake.</param>
    /// <param name="hapticsAmplitude">The amplitude for the player controllers.</param>
    public void ShakeCamera(float intensity, float seconds, float hapticsAmplitude = 0.75f)
    {
        //If the users have Screenshake turned on
        if(PlayerPrefs.GetInt("Screenshake", 1) == 1)
        {
            //Set the amplitude gain of the camera
            CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = intensity;

            //Set values so that the shaking can eventually stop
            shakeTimer = seconds;
            shakeTimerTotal = seconds;
            startingCamIntensity = intensity;
        }

        StartCoroutine(PlayHapticsOnAllControllers(hapticsAmplitude, hapticsAmplitude, seconds));   //Add some haptics to everyone's controllers
    }

    /// <summary>
    /// Resets the camera shake.
    /// </summary>
    public void ResetCameraShake()
    {
        shakeTimer = 0;
        _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
    }

    /// <summary>
    /// Sends a haptic impulse to all controllers.
    /// </summary>
    /// <param name="leftMotorAmplitude">The intensity of the haptics on the left side of the controller.</param>
    /// <param name="rightMotorAmplitude">The intensity of the haptics on the right side of the controller.</param>
    /// <param name="seconds">The duration of the haptics event.</param>
    /// <returns></returns>
    private IEnumerator PlayHapticsOnAllControllers(float leftMotorAmplitude, float rightMotorAmplitude, float seconds)
    {
        foreach (var controller in Gamepad.all)
            controller.SetMotorSpeeds(leftMotorAmplitude, rightMotorAmplitude);

        yield return new WaitForSecondsRealtime(seconds);

        foreach (var controller in Gamepad.all)
            controller.ResetHaptics();
    }

    /// <summary>
    /// The camera event for showing the enemy tanks.
    /// </summary>
    /// <param name="newEnemies">The list of enemies being added to the follow camera.</param>
    /// <returns></returns>
    public IEnumerator ShowEnemyWithCamera(GameObject[] newEnemies)
    {
        //Add enemies to cinematic camera
        foreach(var enemy in newEnemies)
            cinematicTargetGroup.AddMember(enemy.transform, 1, 0);

        SwitchCamera(_cinematicCamera);     //Swap to the cinematic camera
        StartCoroutine(AddToGameCamTargetGroup(newEnemies));    //Add the enemies to the game camera after the cinematic camera is blended

        globalUI.transform.Find("Alarm").gameObject.SetActive(true);    //Play alarm animation

        yield return new WaitForSeconds(gameToCinematicBlendSeconds);

        SwitchCamera(_gameCamera);  //Switch back to game camera after the alarm animation

        _currentActiveCamera = _gameCamera;
    }

    /// <summary>
    /// Freezes the camera and zooms back into the player tank.
    /// </summary>
    /// <param name="seconds">The number of seconds to freeze the camera for.</param>
    /// <returns></returns>
    public IEnumerator BringCameraToPlayer(float seconds)
    {
        FreezeCamera(); //Show the freeze camera

        yield return new WaitForSeconds(seconds);

        //Switch back to the game camera
        SwitchCamera(_gameCamera);
        yield return new WaitForSeconds(cinematicToGameBlendSeconds);

        //Make the cinematic and freeze camera follow the player tank again
        EnableCinematicTargetGroup(cinematicTargetGroup);
        EnableFreezeTargetGroup(cinematicTargetGroup);
    }

    /// <summary>
    /// Freezes the camera in place.
    /// </summary>
    public void FreezeCamera()
    {
        SwitchCamera(_freezeCamera);    //Switches the camera
        DisableFreezeTargetGroup();     //Removes the follow group from the freeze camera
        _freezeCamera.m_Lens.OrthographicSize = _gameCamera.State.Lens.OrthographicSize;    //Sets the size of the freeze camera to the game camera for a seamless transition
    }

    /// <summary>
    /// Adds the list of enemies to the game camera's target group after the cinematic camera has finished blending.
    /// </summary>
    /// <param name="newEnemies">The enemies to add to the game camera target group.</param>
    /// <returns></returns>
    private IEnumerator AddToGameCamTargetGroup(GameObject[] newEnemies)
    {
        yield return new WaitForSeconds(gameToCinematicBlendSeconds);
        foreach(var enemy in newEnemies)
            gameTargetGroup.AddMember(enemy.transform, 1, 0);
    }

    /// <summary>
    /// Switches the current active camera.
    /// </summary>
    /// <param name="newCamera">The new camera to switch to.</param>
    private void SwitchCamera(CinemachineVirtualCamera newCamera)
    {
        //Sets the new camera to the highest priority and the other cameras to a lower priority
        foreach(var cam in cameras)
        {
            if (cam == newCamera)
                cam.Priority = 2;
            else
                cam.Priority = 1;
        }

        _currentActiveCamera = newCamera;
    }

    /// <summary>
    /// Removes an enemy from the camera target groups when it is destroyed.
    /// </summary>
    /// <param name="destroyedEnemy">The destroyed enemy.</param>
    public void RemoveOnDestroy(GameObject destroyedEnemy)
    {
        gameTargetGroup.RemoveMember(destroyedEnemy.transform);
        cinematicTargetGroup.RemoveMember(destroyedEnemy.transform);
    }
}
