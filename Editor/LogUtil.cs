using UnityEngine;

namespace AAGen
{
    public static class LogUtil
    {
        public static void Log(object caller, AagenSettings settings,  LogLevelID logLevel, string message)
        {
            if (settings == null)
            {
                Debug.LogError($"Log failed! Settings = null!");
                return;
            }

            if (logLevel == LogLevelID.OnlyErrors)
            {
                Debug.LogError($"{caller.GetType().Name}: {message}");
                return;
            }

            if (logLevel <= settings.LogLevel)
            {
                Debug.Log($"{caller.GetType().Name}: {message}");
            }
        }
    }
}
