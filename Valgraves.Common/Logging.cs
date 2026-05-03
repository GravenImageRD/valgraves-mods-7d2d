using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Valgraves.Common
{
    public static class Logging
    {
        public static bool SendToChat = false;
        
        private static void LogInternal(Action<string> logAction, string file, int line, string message)
        {
            string fileName = Path.GetFileNameWithoutExtension(file); 
            string logText = $"[{fileName}:{line}] {message}";
            logAction?.Invoke(logText);
            if (Player.Entity != null && SendToChat)
            {
                GameManager.Instance.ChatMessageClient(EChatType.Whisper, Player.EntityId, logText,
                    new List<int>() { Player.EntityId }, EMessageSender.Server,
                    GeneratedTextManager.BbCodeSupportMode.NotSupported);
            }
        }
        
        public static void WriteLine(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(Log.WriteLine, file, line, message);
        }
        
        public static void Warning(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(Log.Warning, file, line, message);
        }
        
        public static void Error(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(Log.Error, file, line, message);
        }
    }
}