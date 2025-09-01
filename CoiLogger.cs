// CoiLogger.cs
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CoiStatsBridge
{
  internal static class CoiLogger
  {
    static MethodInfo _info, _warn, _error;
    static bool _bound;

    static void Bind()
    {
      if (_bound) return;
      _bound = true;
      try
      {
        // Find Mafi.Log in loaded assemblies
        var mafiLog = AppDomain.CurrentDomain.GetAssemblies()
          .Select(a => a.GetType("Mafi.Log", throwOnError: false))
          .FirstOrDefault(t => t != null);

        if (mafiLog != null)
        {
          _info  = mafiLog.GetMethod("Info",    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
               ?? mafiLog.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "Info"    && m.GetParameters().Length == 1);
          _warn  = mafiLog.GetMethod("Warning", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
               ?? mafiLog.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name.Contains("Warn") && m.GetParameters().Length == 1);
          _error = mafiLog.GetMethod("Error",   BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
               ?? mafiLog.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.Name == "Error"   && m.GetParameters().Length == 1);

          // Confirm binding right into the COI log
          try { _info?.Invoke(null, new object[] { "[CoiStatsBridge] Logger bound to Mafi.Log" }); } catch { }
        }
      }
      catch { /* ignore and fallback to Unity */ }
    }

    static void Call(MethodInfo mi, string msg)
    {
      try { mi?.Invoke(null, new object[] { msg }); }
      catch { Debug.Log(msg); }
    }

    public static void Info(string msg)
    {
      Bind();
      msg = "[CoiStatsBridge] " + msg;
      if (_info != null) { Call(_info, msg); return; }
      Debug.Log(msg);
    }

    public static void Warn(string msg)
    {
      Bind();
      msg = "[CoiStatsBridge] " + msg;
      if (_warn != null) { Call(_warn, msg); return; }
      Debug.LogWarning(msg);
    }

    public static void Error(string msg)
    {
      Bind();
      msg = "[CoiStatsBridge] " + msg;
      if (_error != null) { Call(_error, msg); return; }
      Debug.LogError(msg);
    }
  }
}
