using System.Runtime.CompilerServices;
using ReactiveDAG.Core.Engine;
using System.Xml.Linq;

internal class Program
{
      private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private static async Task Main(string[] args)
    {
        //Builder: Constructs a single DAG.
        //DagEngine: Manages execution of individual DAGs(start, stop, pause, resume nodes).
        //Workflow: Orchestrates multiple DAGs, managing their lifecycle as a cohesive unit.


        //
        // Demo of fluent api to manually create a dag with inputs and a function, as well as how to update a cell.
        //
        //var builder = Builder.Create()
        //    .AddInput(6.2, out var cell1)
        //    .AddInput(4, out var cell2)
        //    .AddInput(2, out var cell3)
        //    .AddFunction(inputs =>
        //    {
        //        var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
        //        return sum;
        //    }, out var result)
        //    .Build();

        //cell1.OnValueChanged = newValue => Console.WriteLine($"cell1 value changed to: {newValue}");
        //cell2.OnValueChanged = newValue => Console.WriteLine($"cell2 value changed to: {newValue}");
        //cell3.OnValueChanged = newValue => Console.WriteLine($"cell3 value changed to: {newValue}");

        //Console.WriteLine($"Created cell1: {cell1.Value}, cell2: {cell2.Value}, and cell3: {cell3.Value}");
        //Console.WriteLine($"Sum of cells: {await builder.GetResult<double>(result)}");

        //builder.UpdateInput(cell2, 5);
        //builder.UpdateInput(cell3, 6);

        //Console.WriteLine($"Updated Result: {await builder.GetResult<double>(result)}");

        ////
        //// Demo to show how scheduling functions can work.
        ////
        //Console.WriteLine("\nTesting schedule function");

        //var builder2 = Builder.Create()
        //    .AddInput(2, out var cell4)
        //    .AddInput(2, out var cell5)
        //    .AddInput(2, out var cell6)
        //    .AddScheduledFunction(inputs =>
        //    {
        //        var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
        //        return sum;
        //    }, TimeSpan.FromSeconds(2),
        //    runOnce: false,
        //    out var resultCell)
        //    .Build();

        //await builder2.GetResult<double>(resultCell);

        //cell4.OnValueChanged = newValue => Console.WriteLine($"cell4 value changed to: {newValue}");
        //await Task.Delay(TimeSpan.FromSeconds(8));
        //Console.WriteLine("Subsequent output after cell update during interval scheduled function...");
        //builder2.UpdateInput(cell4, 10);
        //await Task.Delay(TimeSpan.FromSeconds(8));
        //builder2.UpdateInput(cell4, 20);

        //
        // Demo to show using workflows to manage DAGs
        //

        var builder = Builder.Create();
        bool isFirstRun = true;
        var myTask = builder
            .CreateWorkflow("MyMultiplicationWorkflow")
            .AddInput(2.0, out var inputCell)
            .AddScheduledFunction(inputs =>
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Console.WriteLine("Task execution skipped due to cancellation request.");
                        return 0.0; 
                    }
                    var currentValue = Convert.ToDouble(inputs[0]);
                    var newValue = isFirstRun ? currentValue : currentValue * 2;
                    isFirstRun = false;
                    builder.UpdateInput(inputCell, newValue);
                    return newValue;
                }, TimeSpan.FromSeconds(2),
                runOnce: false,
                out var resultCell2)
            .AddDagToCurrentWorkflow()
            .Build()
            .GetResult<double>(resultCell2);

        Console.WriteLine("Running workflow...");

        // Add a delay so the function above can run a little before stopping.
        await Task.Delay(TimeSpan.FromSeconds(8)); 
   
        builder.StopWorkflow();
    

        //builder.ResumeCurrentWorkflow();
        //Console.WriteLine("Workflow resumed.");

        Console.ReadLine();
    }
}
