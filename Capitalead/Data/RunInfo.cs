using System.Collections.Concurrent;

namespace Capitalead.Data;

public class RunInfo
{
    public RunStatus Status { get; set; }
    public Dictionary<string, (long sheetId, string title)[]> Sheets { get; } = new ();
    public ConcurrentBag<string> CompletedClusters { get; } = new();

}

public enum RunStatus
{
    None,
    InProgress,
    Completed
}