using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CoiStatsBridge
{
  /// <summary>Small, allocation-light JSON writer for dictionaries of numbers.</summary>
  internal static class JsonWriter
  {
    public static string BuildPayload(long tsMs, long tick, Dictionary<string,double> resources)
    {
      var sb = new StringBuilder(64 + ((resources?.Count ?? 0) * 24));
      sb.Append('{');
      sb.Append("\"ts\":").Append(tsMs).Append(',');
      sb.Append("\"tick\":").Append(tick).Append(',');
      sb.Append("\"resources\":");
      WriteDict(sb, resources);
      sb.Append('}');
      return sb.ToString();
    }

    static void WriteDict( StringBuilder sb, Dictionary<string,double> dict )
    {
      if (dict == null || dict.Count == 0) { sb.Append("{}"); return; }

      sb.Append('{');
      bool first = true;
      foreach (var kv in dict)
      {
        if (!first) sb.Append(',');
        first = false;
        WriteString(sb, kv.Key);
        sb.Append(':');
        sb.Append(kv.Value.ToString(CultureInfo.InvariantCulture));
      }
      sb.Append('}');
    }

    static void WriteString( StringBuilder sb, string s )
    {
      sb.Append('\"');
      if (s != null)
      {
        for (int i = 0; i < s.Length; i++)
        {
          char ch = s[i];
          switch (ch)
          {
            case '\"': sb.Append("\\\""); break;
            case '\\': sb.Append("\\\\"); break;
            case '\b': sb.Append("\\b");  break;
            case '\f': sb.Append("\\f");  break;
            case '\n': sb.Append("\\n");  break;
            case '\r': sb.Append("\\r");  break;
            case '\t': sb.Append("\\t");  break;
            default:
              if (ch < ' ')
              {
                sb.Append("\\u").Append(((int)ch).ToString("x4"));
              }
              else sb.Append(ch);
              break;
          }
        }
      }
      sb.Append('\"');
    }
  }
}
