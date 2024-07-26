using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof (CinemachineTargetGroup))]
public class PlayerCameraFollowController : MonoBehaviour
{
    [SerializeField] private float playerCameraWeight = 1f;
    [SerializeField] private float playerCameraRadius = 3f;
    [SerializeField] private float maxDistanceFromCenter = 20f;

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

    private void Update()
    {
/*        Vector3 groupCenter = Vector3.zero; //TODO: get the tank's position

        for (int i = 0; i < cinemachineTargetGroup.m_Targets.Length; i++)
        {
            float distance = Vector3.Distance(cinemachineTargetGroup.m_Targets[i].target.position, groupCenter);
            if (distance > maxDistanceFromCenter)
            {
                cinemachineTargetGroup.m_Targets[i].weight = 0; // Set weight to 0 if player is too far
            }
            else
            {
                cinemachineTargetGroup.m_Targets[i].weight = Mathf.Lerp(1, 0, distance / maxDistanceFromCenter); // Smooth transition
            }
        }*/
    }
}
