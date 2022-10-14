using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraEventController : MonoBehaviour
{
    public static CameraEventController instance;

    private CinemachineVirtualCamera _virtualCamera;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }

    // Update is called once per frame
    void Update()
    {
        //_virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(_virtualCamera.m_Lens.OrthographicSize, 50, 1 * Time.deltaTime);
    }

    public float GetCameraFOV()
    {
        return _virtualCamera.m_Lens.OrthographicSize;
    }
}
