using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CameraEventController : MonoBehaviour
{
    public static CameraEventController instance;

    private CinemachineVirtualCamera _currentActiveCamera;

    [SerializeField] private CinemachineVirtualCamera _gameCamera;
    [SerializeField] private CinemachineVirtualCamera _cinematicCamera;
    [SerializeField] private CinemachineVirtualCamera _freezeCamera;
    [SerializeField] private CinemachineTargetGroup gameTargetGroup;
    [SerializeField] private CinemachineTargetGroup cinematicTargetGroup;
    [SerializeField] private GameObject globalUI;

    private float shakeTimer, shakeTimerTotal, startingCamIntensity;

    private float gameToCinematicBlendSeconds = 4;
    private float cinematicToGameBlendSeconds = 2;

    private bool inGame;

    private List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

    private void Awake()
    {
        instance = this;

        cameras.Add(_gameCamera);
        cameras.Add(_cinematicCamera);
        cameras.Add(_freezeCamera);
    }

    private void Start()
    {
        _currentActiveCamera = _gameCamera;
        inGame = true;
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
        {
            _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
        }
    }

    public void EnableCinematicTargetGroup(CinemachineTargetGroup newTargetGroup)
    {
        if (newTargetGroup != null)
            _cinematicCamera.m_Follow = newTargetGroup.transform;
    }

    public void EnableFreezeTargetGroup(CinemachineTargetGroup newTargetGroup)
    {
        if (newTargetGroup != null)
            _freezeCamera.m_Follow = newTargetGroup.transform;
    }

    public void DisableTargetGroup()
    {
        _freezeCamera.m_Follow = null;
    }

    public void ShakeCamera(float intensity, float seconds)
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

        StartCoroutine(PlayHapticsOnAllControllers(seconds));
    }

    public void ResetCameraShake()
    {
        shakeTimer = 0;
        _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
    }

    private IEnumerator PlayHapticsOnAllControllers(float seconds)
    {
        foreach (var controller in Gamepad.all)
            controller.SetMotorSpeeds(0.75f, 0.75f);

        yield return new WaitForSecondsRealtime(seconds);

        foreach (var controller in Gamepad.all)
            controller.ResetHaptics();
    }

    public IEnumerator ShowEnemyWithCamera(GameObject newEnemy)
    {
        cinematicTargetGroup.AddMember(newEnemy.transform, 1, 0);
        SwitchCamera(_cinematicCamera);
        StartCoroutine(AddToGameCamTargetGroup(newEnemy));

        globalUI.transform.Find("Alarm").gameObject.SetActive(true);

        yield return new WaitForSeconds(gameToCinematicBlendSeconds);

        SwitchCamera(_gameCamera);

        _currentActiveCamera = _gameCamera;
    }

    public IEnumerator BringCameraToPlayer(float seconds)
    {
        FreezeCamera();

        yield return new WaitForSeconds(seconds);

        SwitchCamera(_gameCamera);
        yield return new WaitForSeconds(cinematicToGameBlendSeconds);

        EnableCinematicTargetGroup(cinematicTargetGroup);
        EnableFreezeTargetGroup(cinematicTargetGroup);
    }


    public void FreezeCamera()
    {
        SwitchCamera(_freezeCamera);
        DisableTargetGroup();
        _freezeCamera.m_Lens.OrthographicSize = _gameCamera.State.Lens.OrthographicSize;
    }

    private IEnumerator AddToGameCamTargetGroup(GameObject newEnemy)
    {
        yield return new WaitForSeconds(gameToCinematicBlendSeconds);
        gameTargetGroup.AddMember(newEnemy.transform, 1, 0);
    }

    private void SwitchCamera(CinemachineVirtualCamera newCamera)
    {
        foreach(var cam in cameras)
        {
            if (cam == newCamera)
                cam.Priority = 2;
            else
                cam.Priority = 1;
        }

        _currentActiveCamera = newCamera;
    }

    public float GetCameraFOV()
    {
        return Camera.main.orthographicSize;
    }

    public void RemoveOnDestroy(GameObject destroyedEnemy)
    {
        gameTargetGroup.RemoveMember(destroyedEnemy.transform);
        cinematicTargetGroup.RemoveMember(destroyedEnemy.transform);
    }
}
