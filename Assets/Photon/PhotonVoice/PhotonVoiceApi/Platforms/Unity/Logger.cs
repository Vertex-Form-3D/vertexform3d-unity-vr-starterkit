using UnityEngine;

namespace Photon.Voice.Unity
{
    // Unity logger implementation of ILogger for use where VoiceComponent.Logger is not available
    public class Logger : ILogger
    {
        public Logger(LogLevel level = LogLevel.Debug) // Before introducing Trace, it logged everything without the ability to change the level.
        {
            this.Level = level;
        }

        public LogLevel Level { get; set; }

        public void Log(LogLevel level, string fmt, params object[] args)
        {
            if (this.Level >= level)
            {
                if (level >= LogLevel.Info) Debug.LogFormat(fmt, args);
                else if (level == LogLevel.Warning) Debug.LogWarningFormat(fmt, args);
                // anything else is an error
                else Debug.LogErrorFormat(fmt, args);
            }
        }
    }
}
