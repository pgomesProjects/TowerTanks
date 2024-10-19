using Sirenix.OdinInspector;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TowerTanks.Scripts
{
    public class BugReportSubmissionManager : MonoBehaviour
    {
        private readonly string decryptionKey = "+DmMYwtbmiQwYoS0B/1bbw==";
        private string apiInfoFileName;

        //The amount of characters each bug report ID will have
        private const int REPORT_ID_CHAR_COUNT = 10;

        private void Awake()
        {
            apiInfoFileName = Application.dataPath + "/bug_report_api_info.dat";
        }

        [Button]
        public void TestSendReport()
        {
            string decryptedJson = DataEncrypter.DecryptFile(apiInfoFileName, decryptionKey);
            StartCoroutine(SendBugReport(JsonUtility.FromJson<APIInformation>(decryptedJson), new BugReportInfo()));
        }

        /// <summary>
        /// Sends a bug report to the Tower Tanks Trello list.
        /// </summary>
        /// <param name="apiInfo">The API information for the HTTP request.</param>
        /// <param name="bugReportInfo">The object holding all of the bug report information from the user.</param>
        /// <returns></returns>
        public IEnumerator SendBugReport(APIInformation apiInfo, BugReportInfo bugReportInfo)
        {
            //Attempt to get a screenshot of the game
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

            //Generate an ID for the bug report
            string reportID = GenerateID();

            //Get the card API url
            string url = apiInfo.apiURL + "cards";

            //Create the form data for the card
            WWWForm form = new WWWForm();
            form.AddField("key", apiInfo.apiKey);
            form.AddField("token", apiInfo.apiToken);
            form.AddField("idList", apiInfo.listID);

            //Card title
            form.AddField("name", "Bug Report (ID = " + reportID + ")");

            //Card description
            form.AddField("desc", bugReportInfo.gameVersion + "\n\n" + bugReportInfo.description);

            //Add label based on the severity of the bug
            string currentSeverity = "";

            switch (bugReportInfo.bugSeverity)
            {
                case BugSeverity.SEVERE:
                    currentSeverity = apiInfo.severeLabelID;
                    break;
                case BugSeverity.MODERATE:
                    currentSeverity = apiInfo.moderateLabelID;
                    break;
                case BugSeverity.MILD:
                    currentSeverity = apiInfo.mildLabelID;
                    break;
            }

            form.AddField("idLabels", currentSeverity);


            //Send a POST request to create the card
            UnityWebRequest www = UnityWebRequest.Post(url, form);
            yield return www.SendWebRequest();

            //Ensure the creation of the card was successful
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error creating Trello card: " + www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            }

            //Card was created successfully
            else
            {
                Debug.Log("Trello card successfully created.");

                //Get the response data to extract the ID of the new card
                string responseText = www.downloadHandler.text;
                string cardID = GetCardID(responseText);

                // If a screenshot was successfully taken, attach it to the card
                if (screenshot != null)
                    StartCoroutine(AttachFileToCard(apiInfo, cardID, screenshot.EncodeToPNG(), "TowerTanks-Screenshot-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png", "image/png"));

                Destroy(screenshot);

                //Attach the system information to the card
                StartCoroutine(AttachDataToCard(apiInfo, cardID, GameSettings.systemSpecs.DisplaySystemInfo(), "System_Info.txt", "text/plain"));

                GameSettings.CopyToClipboard(reportID);
            }
        }

        /// <summary>
        /// Attaches a string of data to an existing Trello card as a file.
        /// </summary>
        /// <param name="apiInfo">The API information for the HTTP request.</param>
        /// <param name="cardID">The ID of the card to attach to.</param>
        /// <param name="data">The data to attach.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileType">The type of file.</param>
        /// <returns></returns>
        private IEnumerator AttachDataToCard(APIInformation apiInfo, string cardID, string data, string fileName, string fileType)
        {
            // Convert the string data to a byte array
            byte[] fileData = Encoding.UTF8.GetBytes(data);
            yield return StartCoroutine(AttachFileToCard(apiInfo, cardID, fileData, fileName, fileType));
        }

        /// <summary>
        /// Attaches a screenshot to an existing card.
        /// </summary>
        /// <param name="apiInfo">The API information for the HTTP request.</param>
        /// <param name="cardID">The ID of the card to attach to.</param>
        /// <param name="fileData">The file to attach.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileType">The type of file.</param>
        /// <returns></returns>
        private IEnumerator AttachFileToCard(APIInformation apiInfo, string cardID, byte[] fileData, string fileName, string fileType)
        {
            //Get the attachment card API urls
            string url = apiInfo.apiURL + "cards/" + cardID + "/attachments";

            // Create form data for the attachment
            WWWForm form = new WWWForm();
            form.AddField("key", apiInfo.apiKey);
            form.AddField("token", apiInfo.apiToken);

            //Add the bytes of the attachment
            form.AddBinaryData("file", fileData, fileName, fileType);

            //Send a POST request to attach the file
            UnityWebRequest www = UnityWebRequest.Post(url, form);
            yield return www.SendWebRequest();

            //Ensure the attachment was successful
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error attaching file to the Trello card: " + www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            }

            //File was attached successfully
            else
            {
                Debug.Log(fileName + " attached to card successfully.");
            }
        }

        private static readonly string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Generates an ID for the bug report.
        /// </summary>
        /// <returns>Returns an ID of a pre-defined length.</returns>
        private string GenerateID()
        {
            //Seed the randomizer
            Random.InitState(System.DateTime.Now.Millisecond);

            //Append characters randomly to form an ID
            StringBuilder result = new StringBuilder(REPORT_ID_CHAR_COUNT);
            for (int i = 0; i < REPORT_ID_CHAR_COUNT; i++)
                result.Append(characters[Random.Range(0, characters.Length)]);

            return result.ToString();
        }

        /// <summary>
        /// Retrieves the card ID from an HTTP response json file.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>Returns the ID of the card as a string.</returns>
        private string GetCardID(string response)
        {
            int idStartIndex = response.IndexOf("\"id\":\"") + 6;
            int idEndIndex = response.IndexOf("\"", idStartIndex);
            return response.Substring(idStartIndex, idEndIndex - idStartIndex);
        }
    }
}
