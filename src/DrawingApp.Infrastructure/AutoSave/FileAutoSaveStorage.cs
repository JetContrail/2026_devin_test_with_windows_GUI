using System.IO;
using DrawingApp.Core.Application.Ports;

namespace DrawingApp.Infrastructure.AutoSave;

/// <summary>Stores each autosave generation as a sub-directory (named by the generation id) of a root directory.</summary>
public sealed class FileAutoSaveStorage : IAutoSaveStorage
{
    private const string TemporarySuffix = ".tmp";

    public FileAutoSaveStorage(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        RootDirectory = Path.GetFullPath(rootDirectory);
    }

    /// <summary><c>%LOCALAPPDATA%\DrawingApp\AutoSave</c>.</summary>
    public static string DefaultDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DrawingApp", "AutoSave");

    public string RootDirectory { get; }

    public IReadOnlyList<string> ListGenerations()
    {
        if (!Directory.Exists(RootDirectory))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateDirectories(RootDirectory)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(name => !name.EndsWith(TemporarySuffix, StringComparison.OrdinalIgnoreCase)),
        ];
    }

    /// <remarks>Files are written to <c>&lt;id&gt;.tmp</c> first and the directory is renamed when complete.</remarks>
    public void WriteGeneration(string id, IReadOnlyList<AutoSaveFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var target = GenerationPath(id);
        var temporary = target + TemporarySuffix;

        DeleteIfExists(temporary);
        Directory.CreateDirectory(temporary);
        foreach (var file in files)
        {
            ValidateName(file.Name, nameof(files));
            File.WriteAllBytes(Path.Combine(temporary, file.Name), file.Content.ToArray());
        }

        DeleteIfExists(target);
        Directory.Move(temporary, target);
    }

    public void DeleteGeneration(string id) => DeleteIfExists(GenerationPath(id));

    private static void DeleteIfExists(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void ValidateName(string name, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, paramName);
        if (name != Path.GetFileName(name) || name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"'{name}' is not a plain file name.", paramName);
        }
    }

    private string GenerationPath(string id)
    {
        ValidateName(id, nameof(id));
        if (id.EndsWith(TemporarySuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Generation ids must not end with '{TemporarySuffix}'.", nameof(id));
        }

        return Path.Combine(RootDirectory, id);
    }
}
