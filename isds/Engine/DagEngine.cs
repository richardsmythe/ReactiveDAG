
using ReactiveDAG.Core.Models;
using ReactiveDAG.Core.Services;
using ReactiveDAG.Services;

namespace ReactiveDAG.Core.Engine
{
    public enum UpdateMode
    {
        Update,
        RefreshDependencies
    }

    public class DagEngine
    {
        private readonly TaskSchedulingService _taskSchedulingService = new();
        private readonly Dictionary<int, DagNode> _nodes = [];
        private int _nextIndex = 0;

        public int NodeCount => _nodes.Count;

        public async Task<T> GetResult<T>(BaseCell cell)
        {
            if (_nodes.TryGetValue(cell.Index, out var node))
            {
                var result = await node.DeferredComputedNodeValue.Value;
                return (T)result;
            }
            throw new InvalidOperationException("Node not found.");
        }

        public void RemoveNode(BaseCell cell)
        {
            if (_nodes.ContainsKey(cell.Index))
            {
                var dependentCells = GetDependentNodes(cell.Index).ToList();
                foreach (var d in dependentCells)
                {
                    _nodes[d].Dependencies.Remove(cell.Index);
                }
                _nodes.Remove(cell.Index);
            }
        }

        private IEnumerable<int> GetDependentNodes(int index)
            =>
            _nodes.Where(n => n.Value.Dependencies
            .Contains(index))
            .Select(n => n.Key);


        public Cell<T> AddInput<T>(T value)
        {
            var cell = Cell<T>.CreateInputCell(_nextIndex++, value);
            var node = new DagNode(cell, () => Task.FromResult<object>(value));
            _nodes[cell.Index] = node;
            return cell;
        }

        public Cell<TResult> AddFunction<TResult>(
            BaseCell[] cells,
            Func<object[], TResult> function,
            TimeSpan? interval = null,
            bool runOnce = true,
            CancellationToken cancellationToken = default)
        {
            var cell = Cell<TResult>.CreateFunctionCell(_nextIndex++);
            var node = new DagNode(cell, async () =>
            {

                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Task canceled");
                    return default;
                }

                var inputValues = await Task.WhenAll(cells.Select(c => _nodes[c.Index].DeferredComputedNodeValue.Value));

                if (cancellationToken.IsCancellationRequested)
                {
                    return default;
                }

                var result = function(inputValues);
                return result;
            }, _taskSchedulingService, interval, runOnce);

            foreach (var c in cells)
            {
                node.Dependencies.Add(c.Index);
            }

            _nodes[cell.Index] = node;
            return cell;
        }

        public void AddDependency(BaseCell dependentCell, BaseCell dependencyCell)
        {
            var dependentNode = _nodes[dependentCell.Index];
            dependentNode.Dependencies.Add(dependencyCell.Index);
            UpdateAndRefresh(dependentCell.Index, UpdateMode.RefreshDependencies);
        }

        public void RemoveDependency(BaseCell dependentCell, BaseCell dependencyCell)
        {
            var dependentNode = _nodes[dependentCell.Index];
            dependentNode.Dependencies.Remove(dependencyCell.Index);
            UpdateAndRefresh(dependentCell.Index, UpdateMode.RefreshDependencies);
        }

        public void UpdateInput<T>(Cell<T> cell, T value)
        {
            if (!EqualityComparer<T>.Default.Equals(cell.Value, value))
            {
                cell.PreviousValue = cell.Value;
                cell.Value = value;
                var node = _nodes[cell.Index];
                node.DeferredComputedNodeValue = new Lazy<Task<object>>(() => Task.FromResult<object>(value));
                cell.OnValueChanged?.Invoke(value);
                UpdateAndRefresh(cell.Index, UpdateMode.Update);
            }
        }

        private void UpdateAndRefresh(int startIndex, UpdateMode mode)
        {
            var visited = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(startIndex);
            while (stack.Count > 0)
            {
                var index = stack.Pop();
                if (!visited.Add(index)) continue;
                var node = _nodes[index];
                var dependantNodes = GetDependentNodes(index);
                if (mode == UpdateMode.Update)
                {
                    foreach (var dependentNodeIndex in dependantNodes)
                    {
                        node = _nodes[dependentNodeIndex];
                        node.DeferredComputedNodeValue = new Lazy<Task<object>>(node.ComputeNodeValueAsync);
                        stack.Push(dependentNodeIndex);
                    }
                }
                else
                {
                    Console.WriteLine($"Refreshing dependencies starting from index {index}");
                    var dependentNodes = GetDependentNodes(index);
                    foreach (var dependentNodeIndex in dependentNodes)
                    {
                        stack.Push(dependentNodeIndex);
                    }
                }
            }
        }

        public void StartAllNodes()
        {
            foreach (var node in _nodes.Values)
            {
                if (node.State.State == TaskState.Pending)
                {
                    node.Start();
                }
            }
        }
        public void PauseAllNodes()
        {
            foreach (var node in _nodes.Values)
            {
                if (node.State.State == TaskState.Running)
                {
                    node.Pause();
                }
            }
        }

        public void StopAllNodes()
        {
            foreach (var node in _nodes.Values)
            {
                if (node.State.State == TaskState.Running || node.State.State == TaskState.Paused)
                {
                    node.Stop();
                }
            }
        }

        public void ResumeAllNodes()
        {
            foreach (var node in _nodes.Values)
            {
                if (node.State.State == TaskState.Paused)
                {

                    node.Resume();
                }
            }
        }

        public void StartNode(int nodeIndex)
        {
            if (_nodes.TryGetValue(nodeIndex, out var node))
            {
                node.Start();
            }
            else
            {
                throw new InvalidOperationException("Node not found.");
            }
        }

        public void PauseNode(int nodeIndex)
        {
            if (_nodes.TryGetValue(nodeIndex, out var node))
            {
                node.Pause();

            }
            else
            {
                throw new InvalidOperationException("Node not found.");
            }
        }


        public void StopNode(int nodeIndex)
        {
            if (_nodes.TryGetValue(nodeIndex, out var node))
            {
                node.Stop();
            }
            else
            {
                throw new InvalidOperationException("Node not found.");
            }
        }

        public void ResumeNode(int nodeIndex)
        {
            if (_nodes.TryGetValue(nodeIndex, out var node))
            {
                node.Resume();
            }
            else
            {
                throw new InvalidOperationException("Node not found.");
            }
        }

        public bool AreAllNodesCompleted()
        {
            foreach (var node in _nodes.Values)
            {
                if (node.State.State != TaskState.Completed)
                {
                    return false;
                }
            }
            return true;
        }

    }
}