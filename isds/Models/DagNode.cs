using ReactiveDAG.Core.Services;

namespace ReactiveDAG.Core.Models
{
    public class DagNode : DagNodeBase
    {

        public int NodeId { get; private set; }
        public NodeState State { get; private set; }
        private readonly Func<Task<object>> _computeNodeValue;
        private readonly TaskSchedulingService _taskSchedulingService;

        private readonly TimeSpan? _interval;
        private readonly bool _runOnce;
        private CancellationTokenSource _cancellationTokenSource;


        public DagNode(
             BaseCell cell,
             Func<Task<object>> computeValue, TaskSchedulingService taskSchedulingService = null,
             TimeSpan? interval = null,
             bool runOnce = false,
             //ITaskStateSerializer serializer = null,
             CancellationToken? cancellationToken = default)
             : base(cell, computeValue)
        {
            NodeId = cell.Index;
            _computeNodeValue = computeValue;
            //_serializer = serializer;
            _taskSchedulingService = taskSchedulingService;
            _interval = interval;
            _runOnce = runOnce;
            if (cancellationToken != null) _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource((CancellationToken)cancellationToken);
            State = new NodeState(NodeId, TaskState.Pending, null, new List<int>(), cell.Type, cell.Type.GetType().FullName);
        }

        public CancellationToken Token => _cancellationTokenSource.Token;

        public void Start()
        {
            if (State.State == TaskState.Pending || State.State == TaskState.Paused)
            {
                State.State = TaskState.Running;
                _ = ExecuteNodeAsync(); //starts execution on a different thread
            }
            else
            {
                Console.WriteLine($"Node cannot start from state: {State}");
            }
        }



        public void Stop()
        {
            if (State.State != TaskState.Running && State.State != TaskState.Paused) return;
            _cancellationTokenSource.Cancel();
            _taskSchedulingService?.CancelTask(NodeId);
            State.State = TaskState.Stopped;
        }

        //public void Pause()
        //{
        //    if (State.State == TaskState.Running)
        //    {
        //        _cancellationTokenSource.Cancel();
        //        //SaveNodeState(NodeId); 
        //        State.State = TaskState.Paused;
        //    }
        //}

        //public void Resume()
        //{
        //    if (State.State == TaskState.Paused)
        //    {
        //        State.State = TaskState.Running;
        //        _cancellationTokenSource = new CancellationTokenSource();
        //        _ = ExecuteNodeAsync();
        //    }

        //}

        public async Task ExecuteNodeAsync()
        {
            try
            {
                while (true)
                {
                   
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    var result = await ComputeNodeValueAsync();

                    if (result != null)
                    {
                        State.State = TaskState.Completed;
                        break;
                    }

                    await Task.Delay(100, _cancellationTokenSource.Token); 
                }
            }
            catch (OperationCanceledException)
            {
                State.State = TaskState.Paused;
            }
            catch (Exception ex)
            {
                State.State = TaskState.Failed;
                Console.WriteLine($"Node execution failed: {ex.Message}");
            }
        }

        public override async Task<object> ComputeNodeValueAsync()
        {
            _cancellationTokenSource ??= new CancellationTokenSource();
            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                State.State = TaskState.Stopped;
                return null;
            }

            State.State = TaskState.Running;

            if (_interval.HasValue && _taskSchedulingService != null)
            {
                _ = _taskSchedulingService.ScheduleTaskAsync(
                    async () =>
                    {
                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            return null;
                        }

                        return await _computeNodeValue();
                    },
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
            // TODO: Decide on how to store task progress.
            // A partial result is related to the progress of the task. It's the intermediate outcomes that we need here.
            // Need to track the task as it's executed so that it can be retrieved.
            throw new NotImplementedException();
        }

        //public void SaveNodeState(int nodeId)
        //{
        //    var nodeState = new NodeState(
        //        NodeId,
        //        State.State,
        //        GetPartialResult(),
        //        Dependencies.ToList(),
        //        Cell.Type,
        //        Cell.GetType().GenericTypeArguments[0].FullName
        //    );


        //    var serializedNode = TaskStateSerializer.Serialize(nodeState);
        //    var tempStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "TempTaskStorage");
        //    Directory.CreateDirectory(tempStoragePath);
        //    var fileName = Path.Combine(tempStoragePath, $"NodeState_{nodeId}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        //    File.WriteAllText(fileName, serializedNode);
        //}

    }
}
