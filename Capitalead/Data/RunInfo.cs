using System.Collections.Concurrent;

namespace Capitalead.Data;

public class RunInfo
{
    public RunStatus Status { get; set; }
    public Import Import { get; set; }
    public int ClustersCount { get; set; }
    public ConcurrentDictionary<string, long> CompletedClusters { get; } = new();

}

public enum RunStatus
{
    None = 0,
    InProgress = 1,
    Completed = 2,
    Error = 3
}