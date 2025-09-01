// CoiStatsBridgeStartup.cs
using UnityEngine;
using Mafi;

namespace CoiStatsBridge
{
  public static class CoiStatsBridgeStartup
  {
    static GameObject _root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
      try
      {
        if (_root != null) return;

        _root = new GameObject("[CoiStatsBridge]");
        Object.DontDestroyOnLoad(_root);
        _root.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

        _root.AddComponent<StatsBridgeMb>();  // server pinger / sender
        _root.AddComponent<StatsOverlayMb>(); // UI overlay

        Log.Info("[CoiStatsBridge] Attached components (StatsBridgeMb, StatsOverlayMb).");
      }
      catch (System.Exception ex)
      {
        Log.Error("[CoiStatsBridge] Startup failed: " + ex.Message);
      }
    }
  }
}
