using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof (CinemachineTargetGroup))]
public class PlayerCameraFollowController : MonoBehaviour
{
    [SerializeField] private float playerCameraWeight = 1f;
    [SerializeField] private float playerCameraRadius = 3f;

    private CinemachineTargetGroup cinemachineTargetGroup;

    private List<Transform> playerTransforms;

    private void Awake()
    {
        cinemachineTargetGroup = GetComponent<CinemachineTargetGroup>();
        playerTransforms = new List<Transform>();
    }

    public void AddPlayerToTargetGroup(Transform playerTransform)
    {
        cinemachineTargetGroup.AddMember(playerTransform, playerCameraWeight, playerCameraRadius);
        playerTransforms.Add(playerTransform);
    }
}
