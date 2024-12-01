using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class GameAnalytics
    {
        public float timePlayed { get; private set; }

        public GameAnalytics()
        {
            this.timePlayed = 0f;
        }

        public void AddToTimePlayed(float timeElapsed) => timePlayed += timeElapsed;

        public override string ToString()
        {
            string data = "";

            int hours = Mathf.FloorToInt(timePlayed / 3600);
            int minutes = Mathf.FloorToInt(timePlayed % 3600 / 60);
            int seconds = Mathf.FloorToInt(timePlayed % 60);

            if (hours > 0)
                data += string.Format("Time Played: {0:0}:{1:00}:{2:00}", hours, minutes, seconds);
            else
                data += string.Format("Time Played: {0:0}:{1:00}", minutes, seconds);

            return data;
        }
    }


    public class AnalyticsManager : MonoBehaviour
    {
        private GameAnalytics currentGameAnalytics;
        private void Awake()
        {
            currentGameAnalytics = new GameAnalytics();
        }

        private void Update()
        {
            currentGameAnalytics.AddToTimePlayed(Time.unscaledDeltaTime);
        }

        private void OnApplicationQuit()
        {
            Debug.Log(currentGameAnalytics.ToString());
        }
    }
}
