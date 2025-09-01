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
        var mafiLog = AppDomain.CurrentDomain.GetAssemblies()
          .Select(a => a.GetType("Mafi.Log", throwOnError: false))
          .FirstOrDefault(t => t != null);

        if (mafiLog != null)
        {
          _info  = mafia(mi: mafiLog, name: "Info");
          _warn  = mafia(mi: mafiLog, name: "Warning", altContains: "Warn");
          _error = mafia(mi: mafiLog, name: "Error");

          try { _info?.Invoke(null, new object[] { "[CoiStatsBridge] Logger bound to Mafi.Log" }); } catch { }
        }
      }
      catch { /* fallback to Unity */ }
    }

    static MethodInfo mafia(Type mi, string name, string altContains = null)
    {
      return mi.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null)
          ?? mi.GetMethods(BindingFlags.Public | BindingFlags.Static)
               .FirstOrDefault(m => (altContains == null ? m.Name == name : m.Name.Contains(altContains))
                                    && m.GetParameters().Length == 1);
    }

    static void Call(MethodInfo method, string msg)
    {
      try { method?.Invoke(null, new object[] { msg }); }
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
