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
            var state = new NodeState(
                node.NodeId,
                node.State.State,
                node.GetPartialResult(),
                node.Dependencies.ToList(),
                node.Cell.Type,
                node.Cell.GetType().GenericTypeArguments[0].FullName 
            );

            return JsonConvert.SerializeObject(state);
        }

        public DagNode Deserialize(string serializedData)
        {
            var state = JsonConvert.DeserializeObject<NodeState>(serializedData);
            var cellValueType = Type.GetType(state.CellValueType);
            if (cellValueType == null)
            {
                throw new InvalidOperationException("Unknown cell type.");
            }
            var cellType = typeof(Cell<>).MakeGenericType(cellValueType);
            var cellInstance = Activator.CreateInstance(cellType, state.NodeId, state.CellType, null); // eg: Cell<T>(int index, CellType type, T value)
            var node = new DagNode((BaseCell)cellInstance, null, null, null, false, null);

            //node.LoadPartialResult(state.PartialResult);
            node.Dependencies = new HashSet<int>(state.Dependencies);

            return node;
        }

    }
}
