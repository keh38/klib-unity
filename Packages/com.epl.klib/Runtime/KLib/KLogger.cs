using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KLib
{
    public class KLogger : MonoBehaviour
    {
        public enum Level { Default, Verbose };

        private string _logPath;
        private StringBuilder _log;

        public Level MinimumLevel { set; get; } = Level.Default;

        // Make singleton
        private static KLogger _instance;
        public static KLogger Log
        {
            get
            {
                if (_instance == null)
                {
                    GameObject gobj = GameObject.Find("Logger");
                    if (gobj != null)
                    {
                        _instance = gobj.GetComponent<KLogger>();
                    }
                    else
                    {
                        _instance = new GameObject("Logger").AddComponent<KLogger>();
                    }
                    _instance.Init();
                }
                return _instance;
            }
        }

        public static void Debug(string message)
        {
            UnityEngine.Debug.Log("[Debug] " + message);
        }

        public void Init()
        {
            DontDestroyOnLoad(this);
        }

        public void StartLogging(string logPath)
        {
            _logPath = logPath.Replace(".txt", $"-{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt"); ;

            var folder = Path.GetDirectoryName(_logPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _log = new StringBuilder(1000);
            Application.logMessageReceivedThreaded += HandleLog;
        }

        void StopLogging()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
            FlushLog();
        }

        void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
            FlushLog();
        }

        void FlushLog()
        {
            FileIO.AppendTextFile(_logPath, _log.ToString());
            _log.Clear();
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (logString.StartsWith("[Debug]"))
            {
                if (MinimumLevel == Level.Verbose)
                {
                    _log.AppendLine($"{System.DateTime.Now} {logString}");
                }
            }
            else
            {
                _log.AppendLine($"{System.DateTime.Now} [{type}] {logString}");
                if (type == LogType.Error || type == LogType.Exception)
                {
                    _log.AppendLine(stackTrace);
                }
            }

            if (_log.Length > 1000)
            {
                FlushLog();
            }

        }
    }
}