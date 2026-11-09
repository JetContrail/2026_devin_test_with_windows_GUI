using System.IO;
using DrawingApp.Core.Application.Ports;
using DrawingApp.Infrastructure.AutoSave;

namespace DrawingApp.Tests.Infrastructure;

public sealed class FileAutoSaveStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"autosave-{Guid.NewGuid():N}");
    private readonly FileAutoSaveStorage _storage;

    public FileAutoSaveStorageTests() => _storage = new FileAutoSaveStorage(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void DefaultDirectory_IsUnderLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.Equal(Path.Combine(localAppData, "DrawingApp", "AutoSave"), FileAutoSaveStorage.DefaultDirectory);
    }

    [Fact]
    public void ListGenerations_MissingRoot_IsEmpty() => Assert.Empty(_storage.ListGenerations());

    [Fact]
    public void WriteGeneration_CreatesDirectoryWithFiles()
    {
        _storage.WriteGeneration("g1", [new AutoSaveFile("a.png", new byte[] { 1, 2 }), new AutoSaveFile("m.json", new byte[] { 3 })]);

        Assert.Equal(["g1"], _storage.ListGenerations());
        Assert.Equal([1, 2], File.ReadAllBytes(Path.Combine(_root, "g1", "a.png")));
        Assert.Equal([3], File.ReadAllBytes(Path.Combine(_root, "g1", "m.json")));
        Assert.False(Directory.Exists(Path.Combine(_root, "g1.tmp")));
    }

    [Fact]
    public void WriteGeneration_SameId_ReplacesPreviousContent()
    {
        _storage.WriteGeneration("g1", [new AutoSaveFile("old.png", new byte[] { 1 })]);
        _storage.WriteGeneration("g1", [new AutoSaveFile("new.png", new byte[] { 2 })]);

        Assert.Equal(["new.png"], Directory.GetFiles(Path.Combine(_root, "g1")).Select(Path.GetFileName));
    }

    [Fact]
    public void ListGenerations_IgnoresUnfinishedTemporaryDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_root, "g2.tmp"));
        _storage.WriteGeneration("g1", []);

        Assert.Equal(["g1"], _storage.ListGenerations());
    }

    [Fact]
    public void DeleteGeneration_RemovesDirectoryAndIgnoresMissing()
    {
        _storage.WriteGeneration("g1", [new AutoSaveFile("a.png", new byte[] { 1 })]);
        _storage.WriteGeneration("g2", []);

        _storage.DeleteGeneration("g1");
        _storage.DeleteGeneration("missing");

        Assert.Equal(["g2"], _storage.ListGenerations());
    }

    [Theory]
    [InlineData("..")]
    [InlineData(@"..\escape")]
    [InlineData("a/b")]
    [InlineData("g1.tmp")]
    [InlineData(" ")]
    public void InvalidIds_Throw(string id)
    {
        Assert.ThrowsAny<ArgumentException>(() => _storage.WriteGeneration(id, []));
        Assert.ThrowsAny<ArgumentException>(() => _storage.DeleteGeneration(id));
    }

    [Fact]
    public void WriteGeneration_FileNameWithPath_Throws() =>
        Assert.Throws<ArgumentException>(() => _storage.WriteGeneration("g1", [new AutoSaveFile(@"..\x.png", new byte[] { 1 })]));

    [Fact]
    public void Constructor_BlankRoot_Throws() =>
        Assert.ThrowsAny<ArgumentException>(() => new FileAutoSaveStorage(" "));
}
