using System.IO;
using DrawingApp.Core.Application.Ports;

namespace DrawingApp.Tests.Core;

internal sealed class FakeAutoSaveStorage : IAutoSaveStorage, IDisposable
{
    private readonly SemaphoreSlim _written = new(0);

    public SortedDictionary<string, IReadOnlyList<AutoSaveFile>> Generations { get; } = new(StringComparer.Ordinal);

    public Exception? WriteFailure { get; set; }

    public Exception? DeleteFailure { get; set; }

    public int WriteCount { get; private set; }

    public IReadOnlyList<string> ListGenerations()
    {
        lock (Generations)
        {
            // Reverse order: the service must not rely on the storage returning ids sorted.
            return [.. Generations.Keys.Reverse()];
        }
    }

    public void WriteGeneration(string id, IReadOnlyList<AutoSaveFile> files)
    {
        lock (Generations)
        {
            WriteCount++;
            if (WriteFailure is not null)
            {
                throw WriteFailure;
            }

            Generations[id] = files;
        }

        _written.Release();
    }

    public void DeleteGeneration(string id)
    {
        lock (Generations)
        {
            if (DeleteFailure is not null)
            {
                throw DeleteFailure;
            }

            Generations.Remove(id);
        }
    }

    public async Task WaitForWriteAsync()
    {
        if (!await _written.WaitAsync(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("No autosave generation was written.");
        }
    }

    public void Dispose() => _written.Dispose();

    public static IOException IoFailure() => new("disk full");
}
