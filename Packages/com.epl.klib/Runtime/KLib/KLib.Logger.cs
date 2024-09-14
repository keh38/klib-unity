using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KLib
{
    public class Logger : MonoBehaviour
    {
        private string _logPath;
        private StringBuilder _log;

        public void StartLogging(string logPath)
        {
            _logPath = logPath.Replace(".txt", $"-{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}-.txt"); ;

            var folder = Path.GetDirectoryName(_logPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _log = new StringBuilder(1000);
            Application.logMessageReceived += HandleLog;
        }

        void StopLogging()
        {
            Application.logMessageReceived -= HandleLog;
            FlushLog();
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            FlushLog();
        }

        void FlushLog()
        {
            FileIO.AppendTextFile(_logPath, _log.ToString());
            _log.Clear();
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            _log.AppendLine($"{System.DateTime.Now} [{type}] {logString}");
            if (type == LogType.Error || type == LogType.Exception)
            {
                _log.AppendLine(stackTrace);
            }

            if (_log.Length > 1000)
            {
                FlushLog();
            }

        }
    }
}