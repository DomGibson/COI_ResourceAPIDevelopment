using UnityEngine;

namespace CoiStatsBridge
{
  public static class CoiStatsBridgeStartup
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
      var go = new GameObject("CoiStatsBridge");
      Object.DontDestroyOnLoad(go);
      go.AddComponent<StatsBridgeMb>();
      go.AddComponent<StatsOverlayMb>();
    }
  }
}
