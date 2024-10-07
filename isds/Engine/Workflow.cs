using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Core.Engine
{
    public class Workflow
    {
        public Guid WorkflowId { get; set; }
        public string Name { get; set; }
        public TaskState State { get; set; } = TaskState.Pending;
        private List<DagEngine> _dagEngines = new List<DagEngine>();

        public Workflow(string name)
        {
            Name = name;
            WorkflowId = Guid.NewGuid();
        }

        public void AddToWorkflow(DagEngine dag)
        {
            _dagEngines.Add(dag);
        }
        public void RemoveFromWorkflow(DagEngine dag)
        {
            _dagEngines.Remove(dag);
        }


        public void StartWorkflow()
        {
            if (State == TaskState.Pending || State == TaskState.Paused)
            {
                State = TaskState.Running;
                Console.WriteLine($"Workflow '{Name}' started.");

                foreach (var dag in _dagEngines)
                {
                    dag.StartAllNodes();
                }
            }
        }

        public void PauseWorkflow()
        {
            if (State == TaskState.Running)
            {
                State = TaskState.Paused;
                Console.WriteLine($"Workflow '{Name}' paused.");

                foreach (var dag in _dagEngines)
                {
                    dag.PauseAllNodes();
                }
            }
        }

        public void ResumeWorkflow()
        {
            if (State == TaskState.Paused)
            {
                State = TaskState.Running;
                Console.WriteLine($"Workflow '{Name}' resumed.");

                foreach (var dag in _dagEngines)
                {
                    dag.ResumeAllNodes();
                }
            }
        }

        public void CompleteWorkflow()
        {
            if (State == TaskState.Running && AreAllDagsCompleted())
            {
                State = TaskState.Completed;
                Console.WriteLine($"Workflow '{Name}' completed.");
            }
        }

        public void FailWorkflow(Exception ex)
        {
            State = TaskState.Failed;
            Console.WriteLine($"Workflow '{Name}' failed: {ex.Message}");

            foreach (var dag in _dagEngines)
            {
                dag.StopAllNodes();
            }
        }

        public void DeleteWorkflow()
        {
            Console.WriteLine($"Workflow '{Name}' deleted.");
            _dagEngines.Clear();
        }

        private bool AreAllDagsCompleted()
        {
            foreach (var dag in _dagEngines)
            {
                if (!dag.AreAllNodesCompleted())
                {
                    return false;
                }
            }
            return true;
        }

    }
}
