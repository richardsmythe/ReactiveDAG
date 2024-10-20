using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Core.Engine
{
    /// <summary>
    /// Fluent API
    /// </summary>
    public class Builder
    {

        private readonly DagEngine _dagEngine;
        private Workflow _workFlow;
        private readonly List<BaseCell> _cells = [];

        public Builder()
        {
            _dagEngine = new DagEngine();
        }

        public static Builder Create()
        {
            return new Builder();
        }

        public Builder AddInput<T>(T value, out Cell<T> cell)
        {
            cell = _dagEngine.AddInput(value);
            _cells.Add(cell);
            return this;
        }

        public Builder AddInput<T>(T value)
        {
            var cell = _dagEngine.AddInput(value);
            _cells.Add(cell);
            return this;
        }

        public Builder AddFunction<TResult>(Func<object[], TResult> function, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction(_cells.ToArray(), function);
            return this;
        }

        public Builder AddFunction<TResult>(Func<object[], TResult> function)
        {
            _dagEngine.AddFunction(_cells.ToArray(), function);
            return this;
        }

        public Builder AddScheduledFunction<TResult>(Func<object[], TResult> function, TimeSpan interval, bool runOnce, out Cell<TResult> resultCell)
        {
            resultCell = _dagEngine.AddFunction(_cells.ToArray(), function, interval, runOnce);
            return this;
        }

        public Builder AddScheduledFunction<TResult>(Func<object[], TResult> function, TimeSpan interval, bool runOnce)
        {
            _dagEngine.AddFunction(_cells.ToArray(), function, interval, runOnce);
            return this;
        }


        public Builder UpdateInput<T>(Cell<T> cell, T newValue)
        {
            _dagEngine.UpdateInput(cell, newValue);
            return this;
        }

        public DagEngine Build()
        {
            return _dagEngine;
        }

        public Builder StartWorkflow(string workflowName)
        {

            _workFlow = new Workflow(workflowName);

            return this;
        }

        public Builder StopWorkflow()
        {
            
                Console.WriteLine($"Stopping workflow: {_workFlow.Name}");
                _workFlow?.StopWorkflow();
            
            return this;
        }
    

    public Builder AddDagToCurrentWorkflow()
    {
        if (_workFlow == null)
            throw new InvalidOperationException("You must start a workflow before adding DAGs.");

        _workFlow.AddToWorkflow(_dagEngine);
        return this;
    }

    public Builder RunCurrentWorkflow()
    {
        if (_workFlow == null)
            throw new InvalidOperationException("No current workflow to run.");

        _workFlow.StartWorkflow();
        return this;
    }
    public Builder ResetWorkflow()
    {
        _workFlow = null;
        return this;
    }

    public Builder PauseCurrentWorkflow()
    {
        _workFlow?.PauseWorkflow();
        return this;
    }

    //public Builder ResumeCurrentWorkflow()
    //{
    //    _workFlow?.ResumeWorkflow();
    //    return this;
    //}
}
}