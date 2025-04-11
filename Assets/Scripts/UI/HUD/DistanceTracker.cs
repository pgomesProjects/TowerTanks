using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TowerTanks.Scripts
{
    public class DistanceTracker : MonoBehaviour
    {
        [SerializeField, Tooltip("The objective tracker.")] private ObjectiveTracker objectiveTracker;
        [SerializeField, Tooltip("The bar for the distance tracker.")] private RectTransform distanceBar;
        [SerializeField, Tooltip("The tracker for the distance tracker.")] private RectTransform distanceTracker;
        [SerializeField, Tooltip("The text for the distance counter.")] private TextMeshProUGUI meterText; 

        private float distancePercentage, maxDistance;
        private float distanceBarWidth;
        private Vector2 trackerPosition;

        private void Start()
        {
            maxDistance = CampaignManager.Instance.GetCurrentLevelEvent().metersToTravel;
        }

        private void OnEnable()
        {
            distanceBarWidth = distanceBar.sizeDelta.x;
            trackerPosition = distanceTracker.anchoredPosition;
        }

        private void Update()
        {
            if (objectiveTracker == null)
                return;

            //Get the distance percentage (0 to 1)
            distancePercentage = Mathf.Clamp01(objectiveTracker.GetDistanceTraveled() / maxDistance);

            //Move the tracker
            trackerPosition.x = distanceBarWidth * distancePercentage;
            distanceTracker.anchoredPosition = trackerPosition;

            //Update the distance text
            meterText.text = GetMilesText(objectiveTracker.GetDistanceTraveled()) + " / " + GetMilesText(maxDistance);
        }

        private string GetMilesText(float meters)
        {
            //Convert meters to miles
            float miles = Mathf.Floor(meters / 1609.344f * 10f) / 10f;
            //Convert meters to feet
            float feet = Mathf.FloorToInt(meters / 1609.344f * 5280f);

            //Format the string
            if (feet >= 1000f)
                return ((miles % 1 == 0) ? ((int)miles).ToString() : miles.ToString("0.0")) + " mi";
            else
                return feet.ToString() + " ft";
        }
    }
}
