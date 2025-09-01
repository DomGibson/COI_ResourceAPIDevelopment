// StatsOverlayMb.cs
using UnityEngine;

namespace CoiStatsBridge
{
  public sealed class StatsOverlayMb : MonoBehaviour
  {
    Rect _rect = new Rect(40, 40, 360, 220);
    bool _show = true;
    int  _winId;

    GUIStyle _win, _label, _small;
    Texture2D _bg, _titleBg, _dotCircle;

    void Awake()
    {
      _winId = ("CoiStatsBridge.Window".GetHashCode() ^ 0x5A5A5A5A);
    }

    void Update()
    {
      // F9 primary; Alt+F9 fallback
      if (Input.GetKeyDown(KeyCode.F9) || (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.F9)))
      {
        _show = !_show;
        CoiLogger.Info($"F9 pressed. Overlay now {(_show ? "visible" : "hidden")}");
      }
    }

    void OnGUI()
    {
      if (!_show) return;
      EnsureStyles();
      _rect = GUI.Window(_winId, _rect, DrawWindow, GUIContent.none, _win);
    }

    void DrawWindow(int id)
    {
      DrawTitlebar();

      var bridge = FindObjectOfType<StatsBridgeMb>();
      var status = bridge != null ? bridge.Status : StatsBridgeMb.BridgeStatus.Offline;

      GUILayout.Space(8);
      GUILayout.BeginHorizontal();
      DrawStatusDot(status);
      GUILayout.Label("COI Stats Bridge", _label);
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();

      if (bridge != null && !string.IsNullOrEmpty(bridge.LastError))
      {
        GUILayout.Space(6);
        GUILayout.Label("Last error: " + bridge.LastError, _small);
      }

      GUILayout.Space(10);
      GUILayout.Label("F9: toggle • Drag by top bar", _small);

      // Draggable region = whole top strip
      GUI.DragWindow(new Rect(0, 0, Mathf.Infinity, 24));
    }

    // ---------- UI helpers ----------

    void DrawTitlebar()
    {
      var r = GUILayoutUtility.GetRect(10, 20, GUILayout.ExpandWidth(true));
      if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, _titleBg);
      GUI.Label(r, "COI Stats Bridge", _label);
    }

    void DrawStatusDot(StatsBridgeMb.BridgeStatus st)
    {
      Color c = new Color(0.6f, 0.6f, 0.6f, 1f);
      if (st == StatsBridgeMb.BridgeStatus.Online) c = new Color(0.2f, 0.8f, 0.2f, 1f);
      else if (st == StatsBridgeMb.BridgeStatus.Error) c = new Color(0.95f, 0.6f, 0.2f, 1f);
      else if (st == StatsBridgeMb.BridgeStatus.Offline) c = new Color(0.9f, 0.2f, 0.2f, 1f);

      var old = GUI.color;
      GUI.color = c;
      GUILayout.Label(_dotCircle, GUILayout.Width(14), GUILayout.Height(14));
      GUI.color = old;

      GUILayout.Space(6);
    }

    // ---------- styling ----------

    void EnsureStyles()
    {
      if (_win != null) return;

      _bg      = MakeTex(8, 8, new Color32(24, 28, 36, 230));
      _titleBg = MakeTex(8, 8, new Color32(33, 38, 48, 255));
      _dotCircle = MakeCircleTex(14, new Color32(255, 255, 255, 255));

      _win = new GUIStyle(GUI.skin.window);
      _win.normal.background = _bg;
      _win.onNormal.background = _bg;
      _win.padding = new RectOffset(10, 10, 28, 10);

      _label = new GUIStyle(GUI.skin.label) { fontSize = 13 };

      _small = new GUIStyle(GUI.skin.label)
      {
        fontSize = 11,
        normal = { textColor = new Color(0.75f, 0.8f, 0.9f, 1f) }
      };
    }

    Texture2D MakeTex(int w, int h, Color color)
    {
      var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
      var px = new Color[w * h];
      for (int i = 0; i < px.Length; i++) px[i] = color;
      tex.SetPixels(px);
      tex.Apply(false, true);
      return tex;
    }

    Texture2D MakeCircleTex(int d, Color color)
    {
      var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
      int r = d / 2, cx = r, cy = r;
      var px = new Color[d * d];
      for (int y = 0; y < d; y++)
      for (int x = 0; x < d; x++)
      {
        int dx = x - cx, dy = y - cy;
        bool inside = (dx*dx + dy*dy) <= r*r;
        var c = color; c.a = inside ? 1f : 0f;
        px[y * d + x] = c;
      }
      tex.SetPixels(px);
      tex.Apply(false, true);
      return tex;
    }
  }
}
