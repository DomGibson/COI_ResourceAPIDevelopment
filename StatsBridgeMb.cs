using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

namespace CoiStatsBridge
{
  [DisallowMultipleComponent]
  public sealed class StatsBridgeMb : MonoBehaviour
  {
    // Enum now includes Error so the overlay can switch on it.
    public enum BridgeStatus { Unknown, Offline, Online, Error }

    public string ServerUrl = "http://127.0.0.1:3001";

    public struct ResourceSample
    {
      public string id;
      public double balance;
      public double net_per_min;
    }

    public static event Action<Dictionary<string, ResourceSample>> OnNewSample;

    public bool IsOnline { get; private set; }
    public BridgeStatus Status { get; private set; } = BridgeStatus.Unknown;

    public int LastResourceCount { get; private set; }
    public string LastPayload { get; private set; }
    public string LastError { get; private set; }   // <- overlay expects this

    HttpClient _http;
    float _nextProbe;
    bool _probing;
    bool _everLogged;

    void Awake()
    {
      CoiLogger.Info("[CoiStatsBridge] Bridge Awake");
      _http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1000) };
      LastPayload = null;
      LastError = null;
    }

    void OnEnable()
    {
      _nextProbe = 0f;     // kick an immediate probe
      _probing = false;
    }

    void OnDisable()
    {
      var h = _http; _http = null;
      try { h?.Dispose(); } catch {}
      _probing = false;
    }

    void Update()
    {
      var now = Time.realtimeSinceStartup;
      if (now >= _nextProbe && !_probing)
      {
        _nextProbe = now + 2.0f; // poll every 2s
        StartCoroutine(Probe());
      }
    }

    public void ForceProbe() => _nextProbe = 0f;

    IEnumerator Probe()
    {
      if (_http == null) yield break;

      _probing = true;

      // No try/catch here (iterator blocks can’t yield inside a try with catch).
      using (var req = new HttpRequestMessage(HttpMethod.Get, $"{ServerUrl}/v1/resources"))
      {
        var sendTask = _http.SendAsync(req);
        while (!sendTask.IsCompleted) yield return null;

        if (sendTask.IsCanceled || sendTask.IsFaulted)
        {
          LastError = sendTask.Exception?.GetBaseException().Message ?? "Request canceled/faulted";
          SetStatus(BridgeStatus.Error, false);
          _probing = false;
          yield break;
        }

        var resp = sendTask.Result;
        if (!resp.IsSuccessStatusCode)
        {
          LastError = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}";
          SetStatus(BridgeStatus.Error, false);
          _probing = false;
          yield break;
        }

        var bodyTask = resp.Content.ReadAsStringAsync();
        while (!bodyTask.IsCompleted) yield return null;

        var body = bodyTask.Result ?? "{}";
        LastPayload = Truncate(body, 8000);
        LastResourceCount = CountResourceKeys(body);
        OnNewSample?.Invoke(new Dictionary<string, ResourceSample>());

        LastError = null;
        SetStatus(BridgeStatus.Online, true);
      }

      _probing = false;
    }

    void SetStatus(BridgeStatus s, bool online)
    {
      if (Status != s)
      {
        Status = s;
        IsOnline = online;
        CoiLogger.Info($"[CoiStatsBridge] Bridge status: {s}");
      }
      else if (!_everLogged)
      {
        _everLogged = true;
        CoiLogger.Info($"[CoiStatsBridge] Bridge initial status: {s}");
      }
    }

    static string Truncate(string s, int max)
      => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + " …";

    // Cheap count of keys within the top-level "resources" object without bringing a JSON lib.
    static int CountResourceKeys(string json)
    {
      try
      {
        int i = json.IndexOf("\"resources\"", StringComparison.OrdinalIgnoreCase);
        if (i < 0) return 0;
        i = json.IndexOf('{', i);
        if (i < 0) return 0;

        int depth = 1, count = 0;
        for (int j = i + 1; j < json.Length && depth > 0; j++)
        {
          char c = json[j];
          if (c == '{') depth++;
          else if (c == '}') depth--;
          else if (depth == 1 && c == '\"')
          {
            int end = json.IndexOf('\"', j + 1);
            if (end > j)
            {
              int colon = json.IndexOf(':', end + 1);
              if (colon > end) count++;
              j = end;
            }
          }
        }
        return count;
      }
      catch { return 0; }
    }
  }
}
