using System;
using System.Collections.Generic;
using System.Reflection;

namespace Valgraves.Common
{
    public static class Logging
    {
        private static Dictionary<string, string> modNames = new Dictionary<string, string>();

        public static void RegisterMod(string modName)
        {
            string assemblyName = Assembly.GetCallingAssembly().FullName;
            modNames.Add(assemblyName, modName);
        }
        
        public static void WriteLine(string message)
        {
            string modName = modNames[Assembly.GetCallingAssembly().FullName];
            string logText = $"[{modName}] {message}";
            Log.WriteLine(logText);
            if (Player.Entity == null)
            {
                return;
            }
            GameManager.Instance.ChatMessageClient(EChatType.Whisper, Player.EntityId, logText, new List<int>() { Player.EntityId }, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.NotSupported);
        }
        
        public static void Warning(string message)
        {
            string modName = modNames[Assembly.GetCallingAssembly().FullName];
            string logText = $"[{modName}] {message}";
            Log.Warning(logText);
            if (Player.Entity == null)
            {
                return;
            }
            GameManager.Instance.ChatMessageClient(EChatType.Whisper, Player.EntityId, logText, new List<int>() { Player.EntityId }, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.NotSupported);
        }
        
        public static void Error(string message)
        {
            string modName = modNames[Assembly.GetCallingAssembly().FullName];
            string logText = $"[{modName}] {message}";
            Log.Error(logText);
            if (Player.Entity == null)
            {
                return;
            }
            GameManager.Instance.ChatMessageClient(EChatType.Whisper, Player.EntityId, logText, new List<int>() { Player.EntityId }, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.NotSupported);
        }
    }
}