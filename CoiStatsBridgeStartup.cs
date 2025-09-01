using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoiStatsBridge
{
  public static class CoiStatsBridgeStartup
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
      CoiLogger.Info("Startup Init called");
      EnsureBridge();
      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
      CoiLogger.Info($"sceneLoaded: {scene.name} ({mode})");
      EnsureBridge();
    }

    private static void EnsureBridge()
    {
      const string GO_NAME = "CoiStatsBridgeGO";
      var go = GameObject.Find(GO_NAME);
      if (go == null)
      {
        go = new GameObject(GO_NAME);
        Object.DontDestroyOnLoad(go);
      }

      bool addedBridge = false;
      if (go.GetComponent<StatsBridgeMb>() == null) { go.AddComponent<StatsBridgeMb>(); addedBridge = true; }

      StatsOverlayMb.Install();

      if (addedBridge)
        CoiLogger.Info("Attached StatsBridgeMb and installed StatsOverlayMb");
    }
  }
}
