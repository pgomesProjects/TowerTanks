using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class CameraEventController : MonoBehaviour
{
    public static CameraEventController instance;

    private CinemachineBrain cinemachineBrain;
    private CinemachineVirtualCamera _currentActiveCamera;
    [SerializeField] private CinemachineVirtualCamera _gameCamera;
    [SerializeField] private CinemachineVirtualCamera _cinematicCamera;
    [SerializeField] private CinemachineTargetGroup _tanksTargetGroup;
    [SerializeField] private GameObject globalUI;
    private float shakeTimer, shakeTimerTotal, startingCamIntensity;

    private bool inGame;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
        _currentActiveCamera = _gameCamera;
        inGame = true;
    }

    // Update is called once per frame
    void Update()
    {
        //Camera shake timer
        if(shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;

            if(shakeTimer <= 0)
            {
                CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = _currentActiveCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = Mathf.Lerp(startingCamIntensity, 0f, 1 - (shakeTimer / shakeTimerTotal));
            }
        }
    }

    public void EnableTargetGroup(CinemachineTargetGroup newTargetGroup)
    {
        if (newTargetGroup != null)
        {
            _currentActiveCamera.m_Follow = newTargetGroup.transform;
        }
    }

    public void DisableTargetGroup()
    {
        _currentActiveCamera.m_Follow = null;
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

    private IEnumerator PlayHapticsOnAllControllers(float seconds)
    {
        foreach (var controller in Gamepad.all)
            controller.SetMotorSpeeds(0.75f, 0.75f);

        yield return new WaitForSecondsRealtime(seconds);

        foreach (var controller in Gamepad.all)
            controller.ResetHaptics();
    }

    public IEnumerator SmoothZoomCameraEvent(float startFOV, float endFOV, float seconds)
    {
        float timeElapsed = 0;

        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            _gameCamera.m_Lens.OrthographicSize = Mathf.Lerp(startFOV, endFOV, t);
            _cinematicCamera.m_Lens.OrthographicSize = Mathf.Lerp(startFOV, endFOV, t);
            _currentActiveCamera.m_Lens.OrthographicSize = Mathf.Lerp(startFOV, endFOV, t);

            timeElapsed += Time.deltaTime;

            yield return null;
        }

        _gameCamera.m_Lens.OrthographicSize = endFOV;
        _cinematicCamera.m_Lens.OrthographicSize = endFOV;
        _currentActiveCamera.m_Lens.OrthographicSize = endFOV;
    }

    public IEnumerator SmoothMoveCameraEvent(Vector3 startPos, Vector3 endPos, float seconds)
    {
        Vector3 updatedStartPos = new Vector3(startPos.x, startPos.y, Camera.main.transform.position.z);
        Vector3 updatedEndPos = new Vector3(endPos.x, endPos.y, Camera.main.transform.position.z);

        float timeElapsed = 0;
        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            _currentActiveCamera.transform.position = Vector3.Lerp(updatedStartPos, updatedEndPos, t);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        _currentActiveCamera.transform.position = updatedEndPos;
    }

    public IEnumerator ShowEnemyWithCamera(float endFOV, Vector3 enemyPos, float seconds, GameObject newEnemy)
    {
        float blendCamSeconds = 2;
        SwitchCamera();
        _currentActiveCamera = _cinematicCamera;
        StartCoroutine(AddToGameCamTargetGroup(newEnemy, blendCamSeconds));

        DisableTargetGroup();

        globalUI.transform.Find("AlarmOverlay").gameObject.SetActive(true);
        FindObjectOfType<AudioManager>().Play("EnemyAlarm", PlayerPrefs.GetFloat("SFXVolume", 0.5f));

        StartCoroutine(SmoothZoomCameraEvent(GetCameraFOV(), endFOV, seconds));
        StartCoroutine(SmoothMoveCameraEvent(_tanksTargetGroup.transform.position, enemyPos, seconds));

        yield return new WaitForSeconds(seconds);

        globalUI.transform.Find("AlarmOverlay").gameObject.SetActive(false);
        if (FindObjectOfType<AudioManager>().IsPlaying("EnemyAlarm"))
        {
            FindObjectOfType<AudioManager>().Stop("EnemyAlarm");
        }

        SwitchCamera();

        yield return new WaitForSeconds(blendCamSeconds);

        _tanksTargetGroup.AddMember(newEnemy.transform, 1, 0);
        EnableTargetGroup(_tanksTargetGroup);

        _currentActiveCamera = _gameCamera;
    }

    public IEnumerator BringCameraToPlayer(float seconds)
    {
        float blendCamSeconds = 2;

        _cinematicCamera.m_Lens.OrthographicSize = _gameCamera.State.Lens.OrthographicSize;

        SwitchCamera();
        _currentActiveCamera = _cinematicCamera;

        DisableTargetGroup();

        yield return new WaitForSeconds(seconds);

        SwitchCamera();

        yield return new WaitForSeconds(blendCamSeconds);

        EnableTargetGroup(_tanksTargetGroup);
        _currentActiveCamera = _gameCamera;
    }

    private IEnumerator AddToGameCamTargetGroup(GameObject newEnemy, float waitSeconds)
    {
        yield return new WaitForSeconds(waitSeconds);

        if (_tanksTargetGroup != null)
        {
            _tanksTargetGroup.AddMember(newEnemy.transform, 1, 0);
            _gameCamera.m_Follow = _tanksTargetGroup.transform;
        }
    }

    private void SwitchCamera()
    {
        if (inGame)
        {
            _gameCamera.Priority = 1;
            _cinematicCamera.Priority = 2;
            inGame = false;
        }
        else
        {
            _gameCamera.Priority = 2;
            _cinematicCamera.Priority = 1;
            inGame = true;
        }
    }

    public float GetCameraFOV()
    {
        return Camera.main.orthographicSize;
    }
}
