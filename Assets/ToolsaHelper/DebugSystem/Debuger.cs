using System.Diagnostics;
using UnityEngine;

public class Debuger
{
    public static void Log(object context,string message) {
        string className = context.GetType().Name;

        StackTrace stackTrace = new StackTrace(true);
        StackFrame[] stackFrames = stackTrace.GetFrames();

        if (stackFrames.Length >= 2)
        {
            StackFrame callingFrame = stackFrames[1];
            int lineNumber = callingFrame.GetFileLineNumber();

            string filePath = callingFrame.GetFileName();

            string logMessage = $"<color=blue>{className}.cs Line{lineNumber}:</color> <color=black>{message}</color>";
            UnityEngine.Debug.Log(logMessage);
        }
        else
        {
            string logMessage = $"<color=blue>{className}.cs:</color> <color=black>{message}</color>";
            UnityEngine.Debug.Log(logMessage);
        }
    }
}
