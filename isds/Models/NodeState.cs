using Microsoft.VisualBasic;

namespace ReactiveDAG.Core.Models
{
    public class NodeState
    {
        public int NodeId { get; set; }
        public TaskState State { get; set; }
        public object PartialResult { get; set; }
        public List<int> Dependencies { get; set; }
        public CellType CellType { get; set; } 
        public string CellValueType { get; set; }

        public NodeState(int nodeId, TaskState state, object partialResult, List<int> dependencies, CellType cellType, string cellValueType)
        {
            NodeId = nodeId;
            State = state;
            PartialResult = partialResult;
            Dependencies = dependencies;
            CellType = cellType;  
            CellValueType = cellValueType; 
        }
    }
}
