using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoiStatsBridge
{
  /// <summary>
  /// Reflection-only snapshot reader.
  /// - Binds to Mafi.Core.Products.ProductsManager at runtime (no compile-time Mafi refs).
  /// - Enumerates SlimIdManager.ManagedProtos and uses GetStatsFor(proto).GlobalQuantity.Value.
  /// - Safe to build even if your Mafi DLLs aren't referenced during compile.
  /// </summary>
  public static class TypedSnapshotReader
  {
    // Cached binding
    static object _pm;                   // ProductsManager instance
    static Type   _pmType;               // Mafi.Core.Products.ProductsManager
    static MethodInfo _getStatsFor;      // ProductStats GetStatsFor(ProductProto)
    static PropertyInfo _slimIdMgr;      // ProductsSlimIdManager SlimIdManager
    static PropertyInfo _managedProtos;  // ImmutableArray<ProductProto> ManagedProtos
    static PropertyInfo _globalQty;      // Quantity ProductStats.GlobalQuantity
    static PropertyInfo _qtyValue;       // long Quantity.Value
    static PropertyInfo _productProp;    // ProductStats.Product
    static PropertyInfo _protoIdProp;    // ProductProto.Id
    static PropertyInfo _idStringProp;   // ProductProto.ID.String or .Value

    static long _lastBindAttemptMs;

    public static Dictionary<string, double> Read()
    {
      TryBind();

      if (_pm == null)
      {
        DebugState.Provider = "ProductsManager (reflection)";
        DebugState.Method   = "binding pending (load a save)";
        DebugState.Products = 0;
        return new Dictionary<string, double>();
      }

      try
      {
        // protos: pm.SlimIdManager.ManagedProtos (IEnumerable)
        var protosObj = _managedProtos.GetValue(_slimIdMgr.GetValue(_pm));
        var protos = Enumerate(protosObj);
        int count = 0;
        var dict = new Dictionary<string, double>(128);

        foreach (var proto in protos)
        {
          count++;
          var stats = _getStatsFor.Invoke(_pm, new object[] { proto });
          var qtyObj = _globalQty.GetValue(stats);
          var qtyVal = Convert.ToInt64(_qtyValue.GetValue(qtyObj));
          string key = GetProductIdString(proto);

          dict[key] = (double)qtyVal;
        }

        DebugState.Provider = "ProductsManager (reflection)";
        DebugState.Method   = "GetStatsFor(proto).GlobalQuantity.Value";
        DebugState.Products = count;
        return dict;
      }
      catch (Exception ex)
      {
        DebugState.Provider = "ProductsManager (reflection)";
        DebugState.Method   = "EXCEPTION: " + ex.GetType().Name;
        DebugState.Products = 0;
        return new Dictionary<string, double>();
      }
    }

    // -------- binding ----------
    static void TryBind()
    {
      long now = NowMs();
      if (_pm != null) return;
      if (now - _lastBindAttemptMs < 1000) return; // throttle to 1/s
      _lastBindAttemptMs = now;

      var asms  = AppDomain.CurrentDomain.GetAssemblies();
      var types = asms.SelectMany(SafeGetTypes).ToArray();

      // Find ProductsManager type
      _pmType = types.FirstOrDefault(t => t.FullName == "Mafi.Core.Products.ProductsManager")
             ?? types.FirstOrDefault(t => t.Name == "ProductsManager" && (t.Namespace ?? "").Contains("Mafi.Core.Products"));
      if (_pmType == null) { _pm = null; return; }

      // Resolve instance (try any resolver/container or static holder)
      _pm = TryResolveViaAnyResolver(types, _pmType) ?? TryFindStaticInstance(types, _pmType);
      if (_pm == null) return;

      // Cache members
      _getStatsFor = _pmType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .FirstOrDefault(m =>
        {
          if (m.Name != "GetStatsFor") return false;
          var ps = m.GetParameters();
          return ps.Length == 1; // (ProductProto)
        });

      _slimIdMgr = _pmType.GetProperty("SlimIdManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var slimIdType = _slimIdMgr?.PropertyType;
      _managedProtos = slimIdType?.GetProperty("ManagedProtos", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

      // ProductStats & Quantity members via return types
      var productStatsType = _getStatsFor?.ReturnType;
      _globalQty = productStatsType?.GetProperty("GlobalQuantity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var qtyType = _globalQty?.PropertyType;
      _qtyValue = qtyType?.GetProperty("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

      _productProp = productStatsType?.GetProperty("Product", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var productProtoType = _productProp?.PropertyType;

      _protoIdProp = productProtoType?.GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var idType = _protoIdProp?.PropertyType;
      _idStringProp = idType?.GetProperty("String", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                   ?? idType?.GetProperty("Value",  BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

      // minimal sanity
      if (_getStatsFor == null || _slimIdMgr == null || _managedProtos == null || _globalQty == null || _qtyValue == null || _protoIdProp == null)
      {
        // binding incomplete
        _pm = null;
        return;
      }
    }

    static IEnumerable Enumerate(object maybeEnum)
    {
      if (maybeEnum is IEnumerable en) return en;
      return Array.Empty<object>();
    }

    static string GetProductIdString(object productProto)
    {
      try
      {
        var idObj = _protoIdProp.GetValue(productProto);
        if (idObj != null)
        {
          var s = _idStringProp?.GetValue(idObj) as string;
          if (!string.IsNullOrEmpty(s)) return s;
          return idObj.ToString();
        }
      } catch {}
      return productProto?.ToString() ?? "unknown";
    }

    static object TryFindStaticInstance(IEnumerable<Type> allTypes, Type wanted)
    {
      foreach (var t in allTypes)
      {
        try
        {
          foreach (var f in t.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))
          {
            if (wanted.IsAssignableFrom(f.FieldType))
            {
              var val = f.GetValue(null);
              if (val != null) return val;
            }
          }
          foreach (var p in t.GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static))
          {
            if (wanted.IsAssignableFrom(p.PropertyType))
            {
              var val = p.GetValue(null);
              if (val != null) return val;
            }
          }
        } catch {}
      }
      return null;
    }

    static object TryResolveViaAnyResolver(IEnumerable<Type> allTypes, Type targetType)
    {
      var resolvers = allTypes.Where(t =>
        (t.FullName ?? t.Name).IndexOf("Resolver", StringComparison.OrdinalIgnoreCase) >= 0 ||
        (t.FullName ?? t.Name).IndexOf("Container", StringComparison.OrdinalIgnoreCase) >= 0
      ).ToArray();

      // Instance-style resolver: static Current/Instance -> instance Resolve<T>()/Get<T>()
      foreach (var rType in resolvers)
      {
        try
        {
          var holder = GetAnyStaticInstance(rType);
          if (holder == null) continue;

          var methods = rType.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
          foreach (var m in methods)
          {
            if (!m.IsGenericMethod) continue;
            if (m.GetParameters().Length != 0) continue;
            var name = m.Name;
            if (!(name.StartsWith("Resolve", StringComparison.OrdinalIgnoreCase) ||
                  name.StartsWith("Get",     StringComparison.OrdinalIgnoreCase))) continue;

            var g = m.MakeGenericMethod(targetType);
            var obj = g.Invoke(holder, null);
            if (obj != null) return obj;
          }
        } catch {}
      }

      // Static-style resolver: static Resolve<T>()/Get<T>()
      foreach (var rType in resolvers)
      {
        try
        {
          var methods = rType.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
          foreach (var m in methods)
          {
            if (!m.IsGenericMethod) continue;
            if (m.GetParameters().Length != 0) continue;
            var name = m.Name;
            if (!(name.StartsWith("Resolve", StringComparison.OrdinalIgnoreCase) ||
                  name.StartsWith("Get",     StringComparison.OrdinalIgnoreCase))) continue;

            var g = m.MakeGenericMethod(targetType);
            var obj = g.Invoke(null, null);
            if (obj != null) return obj;
          }
        } catch {}
      }

      return null;
    }

    static object GetAnyStaticInstance(Type t)
    {
      try
      {
        var names = new[] { "Current", "Instance", "Default", "Global", "Resolver", "Container", "Services" };
        foreach (var n in names)
        {
          var p = t.GetProperty(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
          if (p != null)
          {
            var v = p.GetValue(null);
            if (v != null) return v;
          }
          var f = t.GetField(n, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
          if (f != null)
          {
            var v = f.GetValue(null);
            if (v != null) return v;
          }
        }
      } catch {}
      return null;
    }

    static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
      try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
    }

    static long NowMs() => (long)(DateTime.UtcNow - new DateTime(1970,1,1)).TotalMilliseconds;
  }
}
