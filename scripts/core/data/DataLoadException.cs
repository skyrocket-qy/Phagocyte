using System;

namespace Phagocyte.Core;

/// <summary>
/// Thrown when a data file is missing, unreadable or fails validation.
/// Policy (Phase 3 decision): always throw — a game without catalogs cannot
/// run, and tests fail fast in dev.
/// </summary>
public sealed class DataLoadException : Exception
{
    public string DataPath { get; }

    public DataLoadException(string dataPath, string message, Exception? inner = null)
        : base($"[DataLoader] {dataPath}: {message}", inner)
    {
        DataPath = dataPath;
    }
}
