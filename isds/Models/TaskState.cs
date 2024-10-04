namespace ReactiveDAG.Core.Models;

public enum TaskState
{
    Pending,
    Running,
    Completed,
    Failed,
    Stopped,
    Paused
}