using System;
using System.Collections.Generic;
using System.Reflection;
using Server;

namespace UOContent.Tests;

// PoisonKinds.Configure registers process-wide singleton instances and cannot be
// called twice. Keep Haven fixtures from changing the state seen by other tests.
internal sealed class HavenPoisonTestScope : IDisposable
{
    private readonly Poison[] _poisons = Poison.Poisons.ToArray();
    private readonly Dictionary<string, Poison> _byName = new(Poison.PoisonsByName);
    private readonly List<(FieldInfo Field, object Value)> _cached = new();
    private bool _disposed;

    internal HavenPoisonTestScope()
    {
        foreach (var field in typeof(PoisonKinds).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(Poison)) { _cached.Add((field, field.GetValue(null))); }
        }
        if (Poison.GetPoison("Regular") != null && Poison.GetPoison("Greater") != null) { return; }
        try
        {
            // Temporarily replace an empty or incomplete fixture registry. Restoring
            // the lazy caches too prevents stale singletons after dictionary cleanup.
            Poison.Poisons.Clear();
            Poison.PoisonsByName.Clear();
            foreach (var cache in _cached) { cache.Field.SetValue(null, null); }
            PoisonKinds.Configure();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        Poison.Poisons.Clear();
        Poison.Poisons.AddRange(_poisons);
        Poison.PoisonsByName.Clear();
        foreach (var pair in _byName) { Poison.PoisonsByName.Add(pair.Key, pair.Value); }
        foreach (var cache in _cached) { cache.Field.SetValue(null, cache.Value); }
    }
}
