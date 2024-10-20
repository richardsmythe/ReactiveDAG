namespace ReactiveDAG.Core.Services
{

    public class TaskSchedulingService
    {
        private readonly Dictionary<int, CancellationTokenSource> _scheduledTasks = new();

        public Task ScheduleTaskAsync(
            Func<Task<object>> taskFunc,
            TimeSpan interval,
            bool runOnce,
            int nodeId)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            _scheduledTasks[nodeId] = cancellationTokenSource;

            return Task.Run(async () =>
            {
                if (runOnce)
                {
                    try
                    {
                        await Task.Delay(interval, cancellationToken); 
                        cancellationToken.ThrowIfCancellationRequested();

                        var result = await taskFunc();
                        CancelTask(nodeId);
                        Console.WriteLine($"Task completed after interval with result: {result}");

                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Task was canceled.");
                    }
                }
                else
                {
                    while (!cancellationToken.IsCancellationRequested) 
                    {
                        try
                        {
                            await Task.Delay(interval, cancellationToken); 

                            cancellationToken.ThrowIfCancellationRequested(); 

                            var result = await taskFunc();
                            Console.WriteLine($"Task completed after interval with result: {result}");

                        }
                        catch (TaskCanceledException)
                        {
                            Console.WriteLine("Task was canceled.");
                        }
                    }
                }
            }, cancellationToken); 
        }


        public void CancelTask(int nodeId)
        {
            if (_scheduledTasks.TryGetValue(nodeId, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
                _scheduledTasks.Remove(nodeId);
            }
        }
    }
}