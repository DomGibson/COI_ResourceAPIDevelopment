// StatsOverlayMb.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoiStatsBridge
{
  /// <summary>
  /// F9 to toggle, drag by the title bar.
  /// </summary>
  public class StatsOverlayMb : MonoBehaviour
  {
    // Window + state
    Rect _rect = new Rect(20, 20, 720, 520);
    Vector2 _scroll;
    bool _show = false; // start hidden; press F9 to open
    const int _winId = unchecked((int)0xC01ABEEF);

    // Latest resources from the bridge
    Dictionary<string, StatsBridgeMb.ResourceSample> _last =
      new Dictionary<string, StatsBridgeMb.ResourceSample>();

    // Cache a bridge reference for status light
    StatsBridgeMb _bridge;

    void Awake()
    {
      // subscribe to live samples from the bridge
      StatsBridgeMb.OnNewSample += OnSample;
      // find bridge once (created by startup)
      _bridge = FindObjectOfType<StatsBridgeMb>();
    }

    void OnDestroy()
    {
      StatsBridgeMb.OnNewSample -= OnSample;
    }

    void OnSample(Dictionary<string, StatsBridgeMb.ResourceSample> s)
    {
      _last = s ?? new Dictionary<string, StatsBridgeMb.ResourceSample>();
    }

    void Update()
    {
      if (Input.GetKeyDown(KeyCode.F9))
      {
        _show = !_show;
        CoiLogger.Info($"[CoiStatsBridge] F9 pressed. Overlay now {(_show ? "visible" : "hidden")}");
        // If we just opened, ask the bridge to probe the server immediately
        if (_show && _bridge != null) _bridge.ForceProbe();
      }

      // Bridge could be created after scene load; refresh cache if missing
      if (_bridge == null) _bridge = FindObjectOfType<StatsBridgeMb>();
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

      // --- Status + debug header ---
      var pad = 12f;
      var lineH = 18f;

      // traffic light at top-right
      DrawStatusLight(new Rect(_rect.width - 24 - pad, 4, 20, 20));

      // optional debug info if you have DebugState defined elsewhere
      SafeLabel(new Rect(pad, 28, _rect.width - pad * 2, lineH),
        Try(() => $"provider: {DebugState.Provider}", "provider: (n/a)"));
      SafeLabel(new Rect(pad, 46, _rect.width - pad * 2, lineH),
        Try(() => $"method:   {DebugState.Method}", "method:   (n/a)"));
      SafeLabel(new Rect(pad, 64, _rect.width - pad * 2, lineH),
        Try(() => $"products: {DebugState.Products}", "products: (n/a)"));

      // --- Resources table ---
      var top = 88f;
      var w = _rect.width - pad * 2;
      var h = _rect.height - top - pad;

      GUILayout.BeginArea(new Rect(pad, top, w, h));
      _scroll = GUILayout.BeginScrollView(_scroll);

      // header
      GUILayout.BeginHorizontal();
      GUILayout.Label("Product", GUILayout.Width(w * 0.55f));
      GUILayout.Label("Balance", GUILayout.Width(w * 0.20f));
      GUILayout.Label("Net/min", GUILayout.Width(w * 0.20f));
      GUILayout.EndHorizontal();

      foreach (var r in _last.Values.OrderBy(v => v.net_per_min).Take(120))
      {
        GUILayout.BeginHorizontal();
        GUILayout.Label(r.id, GUILayout.Width(w * 0.55f));
        GUILayout.Label($"{r.balance:n0}", GUILayout.Width(w * 0.20f));
        GUILayout.Label($"{r.net_per_min:+0.##;-0.##;0}", GUILayout.Width(w * 0.20f));
        GUILayout.EndHorizontal();
      }

      GUILayout.EndScrollView();
      GUILayout.EndArea();
    }

    // --- helpers -------------------------------------------------------------

    void DrawStatusLight(Rect r)
    {
      // default to grey if bridge not ready
      var color = new Color(0.6f, 0.6f, 0.6f);
      var label = "INIT";

      if (_bridge != null)
      {
        if (_bridge.IsOnline) { color = new Color(0.20f, 0.75f, 0.25f); label = "ONLINE"; }
        else                  { color = new Color(0.85f, 0.20f, 0.20f); label = "OFFLINE"; }
      }

      // light
      var prev = GUI.color;
      GUI.color = color;
      GUI.DrawTexture(r, Texture2D.whiteTexture);
      GUI.color = prev;

      // label to the left of the light
      var lr = new Rect(r.x - 70, r.y, 64, r.height);
      GUI.Label(lr, label);
    }

    static void SafeLabel(Rect area, string text)
    {
      GUI.Label(area, text ?? string.Empty);
    }

    static string Try(System.Func<string> f, string fallback)
    {
      try { return f(); } catch { return fallback; }
    }
  }
}
