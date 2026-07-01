using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Push;

/// <summary>
/// Graph-owned store of the latest pushed snapshot per moment. <see cref="Publish{T}"/> copies the
/// caller's span into a per-moment graph-owned buffer (grow-or-reuse), decoupling the caller's array by
/// construction (swap-copy) — no seal machinery. <see cref="Latest"/> hands the current snapshot to the
/// frame-start bind step.
/// </summary>
[PublicAPI]
public sealed class PushStore
{
    private sealed class Entry
    {
        public byte[] Buffer = [];
        public int ByteLength;
    }

    private readonly Dictionary<Identification, Entry> _entries = [];

    /// <summary>Copies <paramref name="data"/>'s bytes into this moment's graph-owned buffer, growing it
    /// only when the payload outgrows current capacity; the caller's array is never retained.</summary>
    public void Publish<T>(Identification moment, ReadOnlySpan<T> data) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(data);
        if (!_entries.TryGetValue(moment, out var entry))
        {
            entry = new Entry();
            _entries[moment] = entry;
        }

        if (entry.Buffer.Length < bytes.Length)
            entry.Buffer = new byte[bytes.Length];

        bytes.CopyTo(entry.Buffer);
        entry.ByteLength = bytes.Length;
    }

    /// <summary>The latest snapshot bytes for <paramref name="moment"/>, or empty when nothing has been
    /// published for it yet.</summary>
    public ReadOnlyMemory<byte> Latest(Identification moment) =>
        _entries.TryGetValue(moment, out var entry)
            ? entry.Buffer.AsMemory(0, entry.ByteLength)
            : ReadOnlyMemory<byte>.Empty;
}
