using System.Resources;
using Newtonsoft.Json;
using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Core.Services
{
    public interface ITaskStateSerializer
    {
        string Serialize(DagNode node);
        DagNode Deserialize(string serializedData);
    }

    public class JsonTaskStateSerializer : ITaskStateSerializer
    {
        public string Serialize(DagNode node)
        {
            var cellValueType = node.Cell.GetType().GenericTypeArguments[0].FullName;

            var state = new NodeState(
                node.NodeId,
                node.State.State,
                node.GetPartialResult(),
                node.Dependencies.ToList(),
                node.Cell.Type,
                cellValueType 
            );

            return JsonConvert.SerializeObject(state);
        }

        public DagNode Deserialize(string serializedData)
        {
            var state = JsonConvert.DeserializeObject<NodeState>(serializedData);
            Type cellValueType = Type.GetType(state.CellValueType);
            if (cellValueType == null)
            {
                throw new InvalidOperationException($"Unknown cell value type: {state.CellValueType}");
            }
            BaseCell cell;
            if (state.CellType == CellType.Input)
            {
                var inputCellType = typeof(Cell<>).MakeGenericType(cellValueType);
                cell = (BaseCell)Activator.CreateInstance(inputCellType, state.NodeId, CellType.Input, null);
            }
            else if (state.CellType == CellType.Function)
            {
                var functionCellType = typeof(Cell<>).MakeGenericType(cellValueType);
                cell = (BaseCell)Activator.CreateInstance(functionCellType, state.NodeId, CellType.Function, null);
            }
            else
            {
                throw new InvalidOperationException("Unknown cell type.");
            }

            Func<Task<object>> computeFunc = async () =>
            {
                return await Task.FromResult<object>(null);
            };

            var node = new DagNode(cell, computeFunc, null, null, false);

            // Load partial result and dependencies
            node.LoadPartialResult(state.PartialResult); // Ensure this method is implemented.
            node.Dependencies = new HashSet<int>(state.Dependencies); // Convert List back to HashSet.
            return node;

  
        }
    }
}
