using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TowerTanks.Scripts
{
    [System.Serializable]
    public class TestFeedback
    {
        public string testDataOne { get; private set; }
        public string testDataTwo { get; private set; }
        public string testDataThree { get; private set; }

        public TestFeedback(string testDataOne, string testDataTwo, string testDataThree)
        {
            this.testDataOne = testDataOne;
            this.testDataTwo = testDataTwo;
            this.testDataThree = testDataThree;
        }
    }

    public class FeedbackSender : MonoBehaviour
    {
        [SerializeField, Tooltip("Test Data 1")] private string testDataOne;
        [SerializeField, Tooltip("Test Data 2")] private string testDataTwo;
        [SerializeField, Tooltip("Test Data 3")] private string testDataThree;

        private const string formUrl = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSdRsVVJzOxcgMFhm1HcxmFklg-R0YdsPfREZH9DnR0GfSG12A/formResponse";

        [Button]
        public void SubmitFeedback()
        {
            StartCoroutine(Post(new TestFeedback(testDataOne, testDataTwo, testDataThree)));
        }

        /// <summary>
        /// Posts the feedback data to the tester Google Form.
        /// </summary>
        /// <param name="testFeedback">The class object that holds all of the tester data.</param>
        /// <returns></returns>
        private IEnumerator Post(TestFeedback testFeedback)
        {
            WWWForm form = new WWWForm();

            //Input the data from the class object into its respective fields
            form.AddField("entry.1836378093", testFeedback.testDataOne);
            form.AddField("entry.1241972641", testFeedback.testDataTwo);
            form.AddField("entry.2124726248", testFeedback.testDataThree);

            using (UnityWebRequest www = UnityWebRequest.Post(formUrl, form))
            {
                //Send the form to the server using an HTTP Post
                yield return www.SendWebRequest();

                if(www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Feedback submitted successfully.");
                }
                else
                {
                    Debug.LogError("Error in feedback submission: " + www.error);
                }
            }
        }
    }
}
