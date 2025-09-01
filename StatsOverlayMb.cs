// StatsOverlayMb.cs
using System;
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
    private Rect _rect = new Rect(40, 40, 720, 520);
    private Vector2 _scroll;
    private bool _show = true; // start visible for sanity-check; press F9 to hide/show
    private const int WinId = unchecked((int)0xC01ABEEF);

    // Optional bridge hook (if present)
    private StatsBridgeMb _bridge;
    private Dictionary<string, StatsBridgeMb.ResourceSample> _last =
      new Dictionary<string, StatsBridgeMb.ResourceSample>();

    // Simple badge styles that DON'T require TextRendering enums
    private GUIStyle _badgeOk, _badgeWarn, _badgeErr, _mono, _header;

    private void Awake()
    {
      DontDestroyOnLoad(gameObject);
      TryBindBridge();
      BuildStyles();
      CoiLogger.Info("[Overlay] Awake");
    }

    private void OnEnable()
    {
      // subscribe once
      StatsBridgeMb.OnNewSample -= OnSample;
      StatsBridgeMb.OnNewSample += OnSample;
    }

    private void OnDisable()
    {
      StatsBridgeMb.OnNewSample -= OnSample;
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.F9))
      {
        _show = !_show;
        CoiLogger.Info("[Overlay] Toggle -> " + (_show ? "ON" : "OFF"));
      }
    }

    private void OnGUI()
    {
      try
      {
        // Also catch F9 here in case game UI swallows Update()
        Event e = Event.current;
        if (e != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.F9)
        {
          _show = !_show;
          e.Use();
          CoiLogger.Info("[Overlay] Toggle (OnGUI) -> " + (_show ? "ON" : "OFF"));
        }

        if (!_show) return;

        _rect = GUI.Window(WinId, _rect, DrawWindow, "COI Resource Bridge (F9 to toggle)");
      }
      catch (Exception ex)
      {
        CoiLogger.Warn("[Overlay] OnGUI exception: " + ex.Message);
      }
    }

    private void DrawWindow(int id)
    {
      try
      {
        if (_mono == null) BuildStyles();

        // Header row
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Captain of Industry → Localhost feed", _header);
        GUILayout.FlexibleSpace();

        var statusPair = GetBridgeStatus();
        DrawStatusBadge(statusPair.Item1);
        GUILayout.EndHorizontal();

        // Server + error
        string url = statusPair.Item2;
        GUILayout.Label("Server: " + url, _mono);
        if (_bridge != null && !string.IsNullOrEmpty(_bridge.LastError))
        {
          GUILayout.Label("Last error: " + _bridge.LastError, _mono);
        }

        // Payload summary if available
        if (_bridge != null && !string.IsNullOrEmpty(_bridge.LastPayload))
        {
          GUILayout.Label("Last payload size: " + _bridge.LastPayload.Length + " chars", _mono);
        }

        GUILayout.Space(6);
        GUILayout.Label("Latest sample", _header);

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(420));
        if (_last != null && _last.Count > 0)
        {
          foreach (var kv in _last.OrderBy(k => k.Key))
          {
            GUILayout.BeginHorizontal();
            // left: resource id
            GUILayout.Label(kv.Key, _mono, GUILayout.Width(260));
            GUILayout.FlexibleSpace();
            // right: numbers
            var s = kv.Value;
            GUILayout.Label(
              "balance: " + s.balance.ToString("0.##") + "   " +
              "net/min: " + s.net_per_min.ToString("0.##"),
              _mono);
            GUILayout.EndHorizontal();
          }
        }
        else
        {
          GUILayout.Label("No samples yet.", _mono);
        }
        GUILayout.EndScrollView();

        // Drag window by title area
        var dragRect = new Rect(0, 0, _rect.width, 24);
        GUI.DragWindow(dragRect);
      }
      catch (Exception ex)
      {
        CoiLogger.Warn("[Overlay] DrawWindow exception: " + ex.Message);
      }
    }

    private void TryBindBridge()
    {
      try
      {
        // Using FindObjectOfType for widest Unity compatibility; warning is fine.
        _bridge = UnityEngine.Object.FindObjectOfType<StatsBridgeMb>();
      }
      catch { _bridge = null; }
    }

    private void OnSample(Dictionary<string, StatsBridgeMb.ResourceSample> s)
    {
      _last = s ?? new Dictionary<string, StatsBridgeMb.ResourceSample>();
    }

    private Tuple<StatsBridgeMb.BridgeStatus, string> GetBridgeStatus()
    {
      if (_bridge == null) TryBindBridge();
      if (_bridge == null) return Tuple.Create(StatsBridgeMb.BridgeStatus.Unknown, "not found");

      try
      {
        string u = _bridge.ServerUrl ?? "n/a";
        return Tuple.Create(_bridge.Status, u);
      }
      catch
      {
        return Tuple.Create(StatsBridgeMb.BridgeStatus.Unknown, "n/a");
      }
    }

    private void BuildStyles()
    {
      // Monospace-like readable label
      _mono = new GUIStyle(GUI.skin.label);
      _mono.richText = false; // safer in IMGUI overlays

      _header = new GUIStyle(GUI.skin.label);
      _header.fontSize = 14; // fontSize does NOT pull TextRendering enums

      _badgeOk = MakeBadge(new Color(0.24f, 0.71f, 0.44f));   // green-ish
      _badgeWarn = MakeBadge(new Color(0.85f, 0.65f, 0.13f)); // golden
      _badgeErr = MakeBadge(new Color(0.80f, 0.36f, 0.36f));  // red-ish
    }

    private GUIStyle MakeBadge(Color c)
    {
      var tex = new Texture2D(1, 1);
      tex.SetPixel(0, 0, c);
      tex.Apply();

      var s = new GUIStyle(GUI.skin.box);
      s.normal.background = tex;
      s.normal.textColor = Color.black;
      // no s.alignment = TextAnchor... (avoids TextRendering module)
      s.padding = new RectOffset(8, 8, 2, 2);
      s.margin  = new RectOffset(4, 4, 2, 2);
      return s;
    }
    private void DrawStatusBadge(StatsBridgeMb.BridgeStatus status)
    {
      GUIStyle s = _badgeWarn;
      string text = "UNKNOWN";

      if (status == StatsBridgeMb.BridgeStatus.Online) { s = _badgeOk; text = "ONLINE"; }
      else if (status == StatsBridgeMb.BridgeStatus.Offline) { s = _badgeWarn; text = "OFFLINE"; }
      else if (status == StatsBridgeMb.BridgeStatus.Error) { s = _badgeErr; text = "ERROR"; }

      GUILayout.Label(text, s, GUILayout.Width(90));
    }
  }
}
