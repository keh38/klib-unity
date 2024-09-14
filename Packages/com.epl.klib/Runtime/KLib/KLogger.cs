using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace KLib
{
    /// <summary>
    /// 
    /// </summary>
    public class KLogger : MonoBehaviour
    {
        public enum Level { Default, Verbose };

        private string _logPath;
        private StringBuilder _log;

        public Level MinimumLevel { set; get; } = Level.Default;
        public float RetainDays { set; get; } = 14;

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

        public static KLogger Create(string logPath, Level minimumLevel, float retainDays)
        {
            Log._logPath = logPath.Replace(".txt", $"-{System.DateTime.Now.ToString("yyyyMMdd")}.txt");
            Log.MinimumLevel = minimumLevel;
            Log.RetainDays = retainDays;

            Log.PurgeLogs(logPath);

            var folder = Path.GetDirectoryName(logPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            Log._log = new StringBuilder(1000);

            return Log;
        }

        public static void Debug(string message)
        {
            UnityEngine.Debug.Log("[Debug] " + message);
        }

        private void Init()
        {
            DontDestroyOnLoad(this);
        }

        public void StartLogging(string logPath)
        {
            Application.logMessageReceivedThreaded += HandleLog;
        }

        public void StopLogging()
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

        void PurgeLogs(string logPath)
        {
            var folder = Path.GetDirectoryName(logPath);
            var pattern = Path.GetFileNameWithoutExtension(logPath) + "-*.txt";

            foreach (var f in Directory.EnumerateFiles(folder, pattern))
            {
                var path = Path.Combine(folder, f);
                var ct = File.GetCreationTime(path);
                if ((System.DateTime.Now - ct).TotalDays > RetainDays)
                {
                    File.Delete(path);
                }
            }
        }
    }
}