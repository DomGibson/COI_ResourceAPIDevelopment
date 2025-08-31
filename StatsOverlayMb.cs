using System;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CoiStatsBridge
{
  public class StatsOverlayMb : MonoBehaviour
  {
    const int _winId = unchecked((int)0xC01ABEEF);
    Rect _rect = new Rect(32, 32, 420, 280);
    bool _show = false;
    Vector2 _scroll;

    void Update()
    {
      if (Input.GetKeyDown(KeyCode.F9)) _show = !_show;
#if ENABLE_INPUT_SYSTEM
      if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) _show = !_show;
#endif
    }

    void OnGUI()
    {
      if (!_show) return;
      _rect = GUI.Window(_winId, _rect, DrawWindow, "COI Stats Bridge (F9 to toggle)");
    }

    void DrawWindow(int id)
    {
      GUILayout.BeginVertical();
      GUILayout.Label($"provider: {DebugState.Provider}");
      GUILayout.Label($"method:   {DebugState.Method}");
      GUILayout.Label($"products: {DebugState.Products}");
      GUILayout.Label($"last post (UTC): {DebugState.LastPostUtc}");
      GUILayout.Label($"tick:     {DebugState.Tick}");

      GUILayout.Space(6);
      GUILayout.Label("Preview (top 15 by qty):");

      var snap = TypedSnapshotReader.Read();
      var top = snap.OrderByDescending(kv => kv.Value).Take(15).ToArray();

      _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
      foreach (var kv in top)
      {
        GUILayout.Label($"{kv.Key}: {kv.Value}");
      }
      GUILayout.EndScrollView();

      GUILayout.Space(6);
      GUI.DragWindow();
      GUILayout.EndVertical();
    }
  }
}
