// StatsOverlayMb.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CoiStatsBridge.StatsBridgeMb;

namespace CoiStatsBridge
{
  /// <summary>
  /// F9 to toggle, drag by the title bar.
  /// </summary>
  public class StatsOverlayMb : MonoBehaviour
  {
    static GameObject _host;
    static StatsOverlayMb _inst;

    bool _show = true;
    Vector2 _scroll;

    // Window state
    Rect _rect = new Rect(20, 20, 720, 520);
    const int _winId = unchecked((int)0xC01ABEEF);

    Dictionary<string, ResourceSample> _last = new();

    public static void Install()
    {
      if (_inst != null) return;
      _host = new GameObject("CoiStatsBridge_StatsOverlay");
      Object.DontDestroyOnLoad(_host);
      _inst = _host.AddComponent<StatsOverlayMb>();
    }

    void Awake()  { StatsBridgeMb.OnNewSample += OnSample; }
    void OnDestroy(){ StatsBridgeMb.OnNewSample -= OnSample; }

    void OnSample(Dictionary<string, ResourceSample> s) => _last = s;

    void Update()
    {
      if (Input.GetKeyDown(KeyCode.F9)) _show = !_show;
    }

    void OnGUI()
    {
      if (!_show) return;
      _rect = GUI.Window(_winId, _rect, DrawWindow, "COI Stats Bridge (F9 to toggle)");
    }

    void DrawWindow(int id)
    {
      // Drag by the title bar (top 24px)
      GUI.DragWindow(new Rect(0, 0, _rect.width, 24));

      // Debug lines (provider/method/products)
      GUI.Label(new Rect(12, 28, _rect.width - 24, 20), $"provider: {DebugState.Provider}");
      GUI.Label(new Rect(12, 46, _rect.width - 24, 20), $"method:   {DebugState.Method}");
      GUI.Label(new Rect(12, 64, _rect.width - 24, 20), $"products: {DebugState.Products}");

      // Table
      var pad = 12f;
      var top = 88f;
      var w = _rect.width - pad * 2;
      var h = _rect.height - top - pad;
      GUILayout.BeginArea(new Rect(pad, top, w, h));
      _scroll = GUILayout.BeginScrollView(_scroll);
      foreach (var r in _last.Values.OrderBy(v => v.net_per_min).Take(120))
      {
        GUILayout.BeginHorizontal();
        GUILayout.Label(r.id, GUILayout.Width(w * 0.55f));
        GUILayout.Label($"bal: {r.balance:n0}", GUILayout.Width(w * 0.20f));
        GUILayout.Label($"net/min: {r.net_per_min:+0.##;-0.##;0}", GUILayout.Width(w * 0.20f));
        GUILayout.EndHorizontal();
      }
      GUILayout.EndScrollView();
      GUILayout.EndArea();
    }
  }
}

