using UnityEngine;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class GameAnalytics
    {
        public float timePlayed { get; private set; }
        public string gameStartTime { get; private set; }

        public GameAnalytics()
        {
            timePlayed = 0f;
            gameStartTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void AddToTimePlayed(float timeElapsed) => timePlayed += timeElapsed;

        public string PrintTimePlayed()
        {
            int hours = Mathf.FloorToInt(timePlayed / 3600);
            int minutes = Mathf.FloorToInt(timePlayed % 3600 / 60);
            int seconds = Mathf.FloorToInt(timePlayed % 60);

            if (hours > 0)
                return string.Format("Time Played: {0:0}:{1:00}:{2:00}", hours, minutes, seconds);
            else
                return string.Format("Time Played: {0:0}:{1:00}", minutes, seconds);
        }

        public override string ToString()
        {
            string data = "";

            data += PrintTimePlayed();

            return data;
        }
    }


    public class AnalyticsManager : MonoBehaviour
    {
        public static GameAnalytics currentGameAnalytics { get; private set; }

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
            CampaignManager.Instance.EndCampaign();
        }
    }
}
