using ReactiveDAG.Core.Services;
using ReactiveDAG.Services;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace ReactiveDAG.Core.Models
{
    public class DagNode : DagNodeBase
    {

        public int NodeId { get; private set; }
        public NodeState State { get; private set; }
        private readonly Func<Task<object>> _computeNodeValue;
        private readonly TaskSchedulingService _taskSchedulingService;
        private readonly ITaskStateSerializer _serializer;
        private readonly TimeSpan? _interval;
        private readonly bool _runOnce;
        private CancellationTokenSource _cancellationTokenSource;


        public DagNode(
             BaseCell cell,
             Func<Task<object>> computeValue, TaskSchedulingService taskSchedulingService = null,
             TimeSpan? interval = null,
             bool runOnce = false,
             ITaskStateSerializer serializer = null)
             : base(cell, computeValue)
        {
            NodeId = cell.Index;
            _computeNodeValue = computeValue;
            _serializer = serializer;
            _taskSchedulingService = taskSchedulingService;
            _interval = interval;
            _runOnce = runOnce;
            _cancellationTokenSource = new CancellationTokenSource();
            State = new NodeState(NodeId, TaskState.Pending, null, new List<int>());
        }

        public void Start()
        {
            if (State.State == TaskState.Pending || State.State == TaskState.Paused)
            {
                State.State = TaskState.Running;
                _ = ExecuteNodeAsync();
            }
            else
            {
                Console.WriteLine($"Node cannot start from state: {State}");
            }
        }

        public void Pause()
        {
            if (State.State != TaskState.Running) return;
            _cancellationTokenSource.Cancel();
            SaveNodeState(NodeId);
            State.State = TaskState.Paused;
        }

        public void Stop()
        {
            if (State.State != TaskState.Running && State.State != TaskState.Paused) return;
            _cancellationTokenSource.Cancel();
            State.State = TaskState.Stopped;
        }


        public void Resume()
        {
            if (State.State != TaskState.Paused) return;
            // TODO: implement restoring a task that was paused
            State.State = TaskState.Running;
            _cancellationTokenSource = new CancellationTokenSource();
            _ = ExecuteNodeAsync();
        }

        private async Task ExecuteNodeAsync()
        {
            try
            {
                while (true)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        State.State = TaskState.Paused;
                        return;
                    }
                    var result = await ComputeNodeValueAsync();
                    if (result != null)
                    {
                        State.State = TaskState.Completed;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                State.State = TaskState.Failed;
                Console.WriteLine($"Node execution failed: {ex.Message}");
            }
        }

        public override async Task<object> ComputeNodeValueAsync()
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                State.State = TaskState.Stopped;
                return null;
            }

            State.State = TaskState.Running;

            if (_interval.HasValue && _taskSchedulingService != null)
            {
                _ = _taskSchedulingService.ScheduleTaskAsync(
                    _computeNodeValue,
                    _interval.Value,
                    _runOnce,
                    Cell.Index
                );

                // Continue with the program flow without waiting for the above scheduled task to finish
                return await _computeNodeValue();
            }
            return await _computeNodeValue();
        }

        internal object GetPartialResult()
        {
            throw new NotImplementedException();
        }

        public void SaveNodeState(int nodeId)
        {
            var serializedNode = _serializer.Serialize(this);
            var tempStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "TempTaskStorage");
            Directory.CreateDirectory(tempStoragePath);
            var fileName = Path.Combine(tempStoragePath, $"NodeState_{nodeId}_{DateTime.Now:yyyyMMdd}.json");
            File.WriteAllText(fileName, serializedNode);
        }

    }
}
