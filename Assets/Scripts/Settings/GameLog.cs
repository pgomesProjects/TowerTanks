using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public class GameLog : MonoBehaviour
    {
        private List<string> logs = new List<string>();

        private void Awake()
        {
            Application.logMessageReceived += AddToLog;
        }

        /// <summary>
        /// Captures any debug logs made.
        /// </summary>
        /// <param name="message">The message of the log.</param>
        /// <param name="stackTrace">The stack trace of where the log came from.</param>
        /// <param name="type">The type of log.</param>
        private void AddToLog(string message, string stackTrace, LogType type)
        {
            logs.Add(type + ": " + message + "\nStack Trace: " + stackTrace);
        }

        public override string ToString()
        {
            string log = "===Logs===\n";

            for(int i = 0; i < logs.Count; i++)
            {
                log += logs[i];
                if (i < logs.Count - 1)
                    log += "\n";
            }

            return log;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= AddToLog;
        }
    }
}
