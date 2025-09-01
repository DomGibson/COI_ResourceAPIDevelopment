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
        SceneManager.sceneLoaded += OnSceneLoaded;
        s_hooked = true;
      }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      CoiLogger.Info($"[CoiStatsBridge] sceneLoaded: {scene.name} ({mode})");
      EnsureBridgeAndOverlay();
    }

    private static void EnsureBridgeAndOverlay()
    {
      // --- Bridge on a stable persistent GO ---
      const string BRIDGE_GO = "CoiStatsBridgeGO";
      var bridgeGo = GameObject.Find(BRIDGE_GO);
      if (bridgeGo == null)
      {
        bridgeGo = new GameObject(BRIDGE_GO);
        Object.DontDestroyOnLoad(bridgeGo);
      }

      bool addedBridge = false;
      if (bridgeGo.GetComponent<StatsBridgeMb>() == null)
      {
        bridgeGo.AddComponent<StatsBridgeMb>();
        addedBridge = true;
      }

      // --- Overlay on its own persistent GO (clean separation/UI toggling with F9) ---
      const string OVERLAY_GO = "CoiStatsOverlay";
      var overlay = Object.FindObjectOfType<StatsOverlayMb>(); // OK to be anywhere
      bool addedOverlay = false;

      if (overlay == null)
      {
        var overlayGo = GameObject.Find(OVERLAY_GO);
        if (overlayGo == null)
        {
          overlayGo = new GameObject(OVERLAY_GO);
          Object.DontDestroyOnLoad(overlayGo);
        }
        overlay = overlayGo.GetComponent<StatsOverlayMb>();
        if (overlay == null)
        {
          overlay = overlayGo.AddComponent<StatsOverlayMb>();
          addedOverlay = true;
        }
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
