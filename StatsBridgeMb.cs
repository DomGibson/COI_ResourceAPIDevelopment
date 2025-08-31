using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoiStatsBridge
{
  public class StatsBridgeMb : MonoBehaviour
  {
    public string Endpoint = "http://127.0.0.1:3001/ingest";
    public float IntervalSec = 1f;

    long _tick;
    float _accum;

    void Update()
    {
      _accum += Time.unscaledDeltaTime;
      if (_accum < IntervalSec) return;
      _accum = 0f;

      var snapshot = TypedSnapshotReader.Read();
      var ts = NowMs();
      var payload = JsonWriter.BuildPayload(ts, _tick, snapshot);
      DebugState.Tick = _tick;
      _tick++;

      try
      {
        var req = (HttpWebRequest)WebRequest.Create(Endpoint);
        req.Method = "POST";
        req.ContentType = "application/json";
        req.Timeout = 100;
        var bytes = Encoding.UTF8.GetBytes(payload);
        using (var rs = req.GetRequestStream()) { rs.Write(bytes, 0, bytes.Length); }
        using (var resp = (HttpWebResponse)req.GetResponse()) {}
        DebugState.LastPostUtc = DateTime.UtcNow.ToString("HH:mm:ss");
      }
      catch (Exception) {}
    }

    static long NowMs() => (long)(DateTime.UtcNow - new DateTime(1970,1,1)).TotalMilliseconds;
  }
}
