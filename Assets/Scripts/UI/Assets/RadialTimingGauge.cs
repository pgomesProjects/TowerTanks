using UnityEngine;

namespace TowerTanks.Scripts
{
    [ExecuteInEditMode]
    public class RadialTimingGauge : TimingGauge
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void ActivateTimer()
        {
            tickBar.eulerAngles = Vector3.zero;
            timingActive = true;
        }

        public override void UpdateTargetZoneRange(Vector2 zoneRange)
        {
            //Get the desired width of the zone
            float range = zoneRange.y - zoneRange.x;
            zoneBarImage.fillAmount = range;

            float leftOffset = 360f * zoneRange.x;
            zoneBar.eulerAngles = new Vector3(0f, 0f, -leftOffset);
        }

        protected override void MoveTickBar()
        {
            // Calculate the amount to rotate based on time passed
            float rotationAmount = 360f * (Time.deltaTime / tickSpeed);

            // Apply the rotation to the Z-axis
            tickBar.Rotate(0f, 0f, -rotationAmount);
        }

        protected override float GetTickBarPosition()
        {
            return 1 - (tickBar.eulerAngles.z / 360f);
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}
