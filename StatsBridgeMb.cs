// StatsBridgeMb.cs
using UnityEngine;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using Mafi;

namespace CoiStatsBridge
{
  public sealed class StatsBridgeMb : MonoBehaviour
  {
    public enum BridgeStatus { Offline, Online, Error }

    [Header("Server")]
    public string BaseUrl = "http://127.0.0.1:3001"; // change if needed
    public float  PingIntervalSec = 2f;

    [Header("State")]
    public BridgeStatus Status = BridgeStatus.Offline;
    public string LastError = "";

    HttpClient _http;

    void Awake()
    {
      _http = new HttpClient();
      _http.Timeout = System.TimeSpan.FromMilliseconds(800);
    }

    void OnEnable()
    {
      StartCoroutine(PingLoop());
    }

    IEnumerator PingLoop()
    {
      while (true)
      {
        var t = PingOnce();
        while (!t.IsCompleted) yield return null;
        yield return new WaitForSeconds(PingIntervalSec);
      }
    }

    Task PingOnce()
    {
      return Task.Run(async () =>
      {
        try
        {
          var resp = await _http.GetAsync(BaseUrl + "/health").ConfigureAwait(false);
          Status = resp.IsSuccessStatusCode ? BridgeStatus.Online : BridgeStatus.Offline;
          LastError = "";
        }
        catch (System.Exception ex)
        {
          Status = BridgeStatus.Offline;
          LastError = ex.GetType().Name;
        }
      });
    }

    // (Optional) put your /ingest POST sender here later.
  }
}
