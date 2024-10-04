using ReactiveDAG.Core.Engine;
using System.Xml.Linq;
// TODO - Allows external events such as file uploads, HTTP requests, or database updates to trigger the execution of tasks, making the engine flexible and reactive to changes outside the graph
// TODO - Inject a custom middleware to execute logic before or after computations/workflows
// TODO - Integrate persistence
internal class Program
{
    private static async Task Main(string[] args)
    {
        //Builder: Constructs a single DAG.
        //DagEngine: Manages execution of individual DAGs(start, stop, pause, resume nodes).
        //Workflow: Orchestrates multiple DAGs, managing their lifecycle as a cohesive unit.


        //
        // Demo of fluent api to manually create a dag with inputs and a function, as well as how to update a cell.
        //
        var builder = Builder.Create()
            .AddInput(6.2, out var cell1)
            .AddInput(4, out var cell2)
            .AddInput(2, out var cell3)
            .AddFunction(inputs =>
            {
                var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
                return sum;
            }, out var result)
            .Build();

        cell1.OnValueChanged = newValue => Console.WriteLine($"cell1 value changed to: {newValue}");
        cell2.OnValueChanged = newValue => Console.WriteLine($"cell2 value changed to: {newValue}");
        cell3.OnValueChanged = newValue => Console.WriteLine($"cell3 value changed to: {newValue}");

        Console.WriteLine($"Created cell1: {cell1.Value}, cell2: {cell2.Value}, and cell3: {cell3.Value}");
        Console.WriteLine($"Sum of cells: {await builder.GetValueAsync<double>(result)}");

        builder.UpdateInput(cell2, 5);
        builder.UpdateInput(cell3, 6);

        Console.WriteLine($"Updated Result: {await builder.GetValueAsync<double>(result)}");

        //
        // Demo to show how scheduling functions can work.
        //
        Console.WriteLine("\nTesting schedule function");

        var builder2 = Builder.Create()
            .AddInput(2, out var cell4)
            .AddInput(2, out var cell5)
            .AddInput(2, out var cell6)
            .AddScheduledFunction(inputs =>
            {
                var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
                return sum;
            }, TimeSpan.FromSeconds(2),
            runOnce: false,
            out var resultCell)
            .Build();

        await builder2.GetValueAsync<double>(resultCell);

        cell4.OnValueChanged = newValue => Console.WriteLine($"cell4 value changed to: {newValue}");
        await Task.Delay(TimeSpan.FromSeconds(8));
        Console.WriteLine("Subsequent output after cell update during interval scheduled function...");
        builder2.UpdateInput(cell4, 10);
        await Task.Delay(TimeSpan.FromSeconds(8));
        builder2.UpdateInput(cell4, 20);

        //
        // Demo to show how Workflows can be created to manage multiple DAGs.
        //

        Console.ReadLine();
    }
}
