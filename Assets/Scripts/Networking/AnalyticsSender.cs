using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TowerTanks.Scripts
{
    public class AnalyticsSender : MonoBehaviour
    {
        private const string formUrl = "https://docs.google.com/forms/u/0/d/e/1FAIpQLScPMUXueh3d4zBsrcYZMrct4m3XGMK50qX0aybKJZEpnmhMLg/formResponse";

        public static bool dataSent = false;

        public void SubmitAnalytics()
        {
            //If the data has already been sent, return
            if (dataSent)
                return;

            Debug.Log("Submitting Data...");
            StartCoroutine(PostAnalytics(AnalyticsManager.currentGameAnalytics, GameManager.Instance.currentSessionStats));
        }

        /// <summary>
        /// Posts the analytics online onto a Google Sheet.
        /// </summary>
        /// <param name="gameAnalytics">The general game analytics.</param>
        /// <param name="sessionStats">The current game session statistics.</param>
        /// <returns></returns>
        private IEnumerator PostAnalytics(GameAnalytics gameAnalytics, SessionStats sessionStats)
        {
            WWWForm form = new WWWForm();

            //Input the data from the analytics into their respective fields
            form.AddField("entry.910137412", gameAnalytics.PrintTimePlayed());
            form.AddField("entry.1713432671", gameAnalytics.gameStartTime);

            form.AddField("entry.942374160", sessionStats.maxHeight.ToString("F2"));
            form.AddField("entry.211050498", sessionStats.roomsBuilt.ToString());
            form.AddField("entry.1475134719", sessionStats.totalCells.ToString());
            
            form.AddField("entry.967543880", sessionStats.cargoSold.ToString());
            
            form.AddField("entry.1358898225", sessionStats.cannonsBuilt.ToString());
            form.AddField("entry.1258933251", sessionStats.machineGunsBuilt.ToString());
            form.AddField("entry.1381269811", sessionStats.mortarsBuilt.ToString());
            form.AddField("entry.1888514333", sessionStats.boilersBuilt.ToString());
            form.AddField("entry.1631825548", sessionStats.throttlesBuilt.ToString());

            form.AddField("entry.247855306", sessionStats.enemiesKilled.ToString());

            using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
            {
                //Send the form to the server using an HTTP Post
                yield return www.SendWebRequest();

                if(www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Analytics submitted successfully.");
                    dataSent = true;
                }
                else
                {
                    Debug.LogError("Error in analytics submission: " + www.error);
                    dataSent = false;
                }
            }
        }
    }
}
