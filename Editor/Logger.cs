using UnityEngine;

namespace AAGen
{
    public class Logger
    {
        readonly DataContainer m_DataContainer;
        
        public Logger(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
        }

        public void Log(object invoker, LogLevelID logLevel, string message)
        {
            if (m_DataContainer.Settings == null)
            {
                Debug.LogError($"Log failed! Settings = null!");
                return;
            }

            if (logLevel == LogLevelID.OnlyErrors)
            {
                Debug.LogError($"{invoker.GetType().Name}: {message}");
                return;
            }

            if (logLevel <= m_DataContainer.Settings.LogLevel)
            {
                Debug.Log($"{invoker.GetType().Name}: {message}");
            }
        }
        
        public void LogError(object invoker, string message)
        {
            Log(invoker, LogLevelID.OnlyErrors, message);
        }
        
        public void LogInfo(object invoker, string message)
        {
            Log(invoker, LogLevelID.Info, message);
        }
        
        public void LogDev(object invoker, string message)
        {
            Log(invoker, LogLevelID.Developer, message);
        }
    }
}
