// CoiStatsBridgeStartup.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoiStatsBridge
{
  public static class CoiStatsBridgeStartup
  {
    private static bool s_hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
      CoiLogger.Info("[CoiStatsBridge] Startup Init called");
      EnsureBridgeAndOverlay();

      if (!s_hooked)
      {
        SceneManager.sceneLoaded += (_, __) => EnsureBridgeAndOverlay();
        s_hooked = true;
      }
    }

    private static void EnsureBridgeAndOverlay()
    {
      var root = GameObject.Find("[CoiStatsBridge]") ?? new GameObject("[CoiStatsBridge]");
      Object.DontDestroyOnLoad(root);

      var bridge  = root.GetComponent<StatsBridgeMb>();
      var overlay = root.GetComponent<StatsOverlayMb>();

      bool addedBridge = false, addedOverlay = false;

      if (bridge == null)
      {
        bridge = root.AddComponent<StatsBridgeMb>();
        addedBridge = true;
      }

      if (overlay == null)
      {
        overlay = root.AddComponent<StatsOverlayMb>();
        addedOverlay = true;
      }

      if (addedBridge || addedOverlay)
      {
        CoiLogger.Info($"[CoiStatsBridge] Attached components: " +
                       $"{(addedBridge ? "StatsBridgeMb " : "")}" +
                       $"{(addedOverlay ? "StatsOverlayMb" : "")}".Trim());
      }
    }
  }
}
