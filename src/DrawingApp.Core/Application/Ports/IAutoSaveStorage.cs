namespace DrawingApp.Core.Application.Ports;

/// <summary>Stores autosave generations. Each generation is a set of files identified by an id.</summary>
public interface IAutoSaveStorage
{
    /// <summary>Returns the ids of the stored generations (any order).</summary>
    IReadOnlyList<string> ListGenerations();

    /// <summary>Writes all files of a generation; a generation must never be visible half-written.</summary>
    /// <exception cref="IOException">The generation cannot be written.</exception>
    void WriteGeneration(string id, IReadOnlyList<AutoSaveFile> files);

    /// <exception cref="IOException">The generation cannot be deleted.</exception>
    void DeleteGeneration(string id);
}
