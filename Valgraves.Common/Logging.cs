using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Valgraves.Common
{
    /// <summary>
    /// Helpers for logging.
    /// </summary>
    public static class Logging
    {
        private static bool _debugLogging = false;
        
        /// <summary>
        /// If set, all logging will also go to in game chat.
        /// </summary>
        public static bool LogToChat = false;
        
        /// <summary>
        /// Turns on debug logging, enabling info and warning messages.
        /// </summary>
        public static void EnableDebugLogging()
        {
            _debugLogging = true;
        }
        
        private static void LogInternal(Action<string> logAction, string file, int line, string message)
        {
            string fileName = Path.GetFileNameWithoutExtension(file); 
            string logText = $"[{fileName}:{line}] {message}";
            logAction?.Invoke(logText);
            if (Player.Entity != null && LogToChat)
            {
                GameManager.Instance.ChatMessageClient(EChatType.Whisper, Player.EntityId, logText,
                    new List<int>() { Player.EntityId }, EMessageSender.Server,
                    GeneratedTextManager.BbCodeSupportMode.NotSupported);
            }
        }
        
        /// <summary>
        /// Logs out an informational message. Disabled by default.
        /// </summary>
        public static void WriteLine(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (_debugLogging)
            {
                LogInternal(Log.WriteLine, file, line, message);
            }
        }
        
        /// <summary>
        /// Logs out a warning message. Disabled by default.
        /// </summary>
        public static void Warning(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (_debugLogging)
            {
                LogInternal(Log.Warning, file, line, message);
            }
        }
        
        /// <summary>
        /// Logs out an error message.
        /// </summary>
        public static void Error(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            LogInternal(Log.Error, file, line, message);
        }
    }
}