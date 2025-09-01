// CoiStatsBridgeStartup.cs
using UnityEngine;

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

        _root.AddComponent<StatsBridgeMb>();  // server pinger / (later) sender
        _root.AddComponent<StatsOverlayMb>(); // UI overlay

        Debug.Log("[CoiStatsBridge] Attached StatsBridgeMb + StatsOverlayMb");
      }
      catch (System.Exception ex)
      {
        Debug.LogError("[CoiStatsBridge] Startup failed: " + ex.Message);
      }
    }
  }
}
