namespace DrawingApp.Core.Application.Ports;

/// <summary>One file of an autosave generation.</summary>
public sealed record AutoSaveFile(string Name, ReadOnlyMemory<byte> Content);
