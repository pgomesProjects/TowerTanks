using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraEventController : MonoBehaviour
{
    public static CameraEventController instance;

    private CinemachineVirtualCamera _virtualCamera;
    private CinemachineTargetGroup _targetGroup;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _targetGroup = FindObjectOfType<CinemachineTargetGroup>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void EnableTargetGroup()
    {
        if (_targetGroup != null)
        {
            _virtualCamera.m_Follow = _targetGroup.transform;
        }
    }

    public void DisableTargetGroup()
    {
        _virtualCamera.m_Follow = null;
    }

    public IEnumerator SmoothZoomCameraEvent(float startFOV, float endFOV, float seconds)
    {
        float timeElapsed = 0;

        while (timeElapsed < seconds)
        {
            //Smooth lerp duration algorithm
            float t = timeElapsed / seconds;
            t = t * t * (3f - 2f * t);

            _virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(startFOV, endFOV, t);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        _virtualCamera.m_Lens.OrthographicSize = endFOV;
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

            _virtualCamera.transform.position = Vector3.Lerp(updatedStartPos, updatedEndPos, t);
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        _virtualCamera.transform.position = updatedEndPos;
    }

    public IEnumerator ShowEnemyWithCamera(float endFOV, Vector3 enemyPos, float seconds)
    {
        DisableTargetGroup();

        StartCoroutine(SmoothZoomCameraEvent(GetCameraFOV(), endFOV, seconds));
        StartCoroutine(SmoothMoveCameraEvent(_targetGroup.transform.position, enemyPos, seconds));

        yield return new WaitForSeconds(seconds);

/*        float backToPlayerSeconds = 0.25f;

        StartCoroutine(SmoothMoveCameraEvent(enemyPos, _targetGroup.transform.position, backToPlayerSeconds));
        yield return new WaitForSeconds(backToPlayerSeconds);*/
    }

    public float GetCameraFOV()
    {
        return _virtualCamera.m_Lens.OrthographicSize;
    }
}
