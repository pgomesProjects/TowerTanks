using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class GameAnalytics
    {
        public System.DateTime gameStartTime { get; private set; }
        public List<(System.DateTime, System.DateTime)> campaignTimes { get; private set; }

        public GameAnalytics()
        {
            gameStartTime = System.DateTime.Now;
            campaignTimes = new List<(System.DateTime, System.DateTime)>();
        }

        /// <summary>
        /// Adds a campaign time to the game analytics.
        /// </summary>
        /// <param name="start">The starting timestamp of the campaign.</param>
        /// <param name="end">The ending timestamp of the campaign.</param>
        public void AddCampaignTime(System.DateTime start, System.DateTime end)
        {
            campaignTimes.Add((start, end));
        }

        /// <summary>
        /// Prints the time elapsed from the session.
        /// </summary>
        /// <returns>A readable version of the time played.</returns>
        public string PrintSessionTime() => "Time Played: " + FormatTime(System.DateTime.Now - gameStartTime);

        /// <summary>
        /// Formats time into a readable string.
        /// </summary>
        /// <param name="timeSpan">The time span to show.</param>
        public static string FormatTime(System.TimeSpan timeSpan) => FormatTime((float)timeSpan.TotalSeconds);

        /// <summary>
        /// Formats time into a readable string.
        /// </summary>
        /// <param name="timeSeconds">The total time to show in seconds.</param>
        public static string FormatTime(float timeSeconds)
        {
            int hours = Mathf.FloorToInt(timeSeconds / 3600);
            int minutes = Mathf.FloorToInt(timeSeconds % 3600 / 60);
            int seconds = Mathf.FloorToInt(timeSeconds % 60);

            if (hours > 0)
                return string.Format("{0:0}:{1:00}:{2:00}", hours, minutes, seconds);
            else
                return string.Format("{0:0}:{1:00}", minutes, seconds);
        }

        public override string ToString()
        {
            string data = "";

            data += PrintSessionTime() + "\n";

            //If there are campaign times stored, print the time
            if(campaignTimes.Count > 0)
            {
                data += "===Campaign Times===\n";
                for(int i = 0; i < campaignTimes.Count; i++)
                    data += "Campaign " + (i+1).ToString() + ": " + FormatTime(campaignTimes[i].Item2 - campaignTimes[i].Item1) + "\n";
            }

            return data;
        }
    }


    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }
        public static GameAnalytics currentGameAnalytics { get; private set; }

        private void Awake()
        {
            //Singleton-ize the script
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;

            //Start the analytics system
            currentGameAnalytics = new GameAnalytics();
        }
    }
}
