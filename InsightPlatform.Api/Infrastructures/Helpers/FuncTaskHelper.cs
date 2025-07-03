using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class FuncTaskHelper
{
    public static async Task RunWithRetry(Func<Task> job, TimeSpan delayTs, int maxRetries = 3)
    {
        int retries = 0;

        while (retries < maxRetries)
        {
            try
            {
                await job();
                return;
            }
            catch
            {
                retries++;

                if (retries >= maxRetries)
                {
                    throw;
                }

                await Task.Delay(delayTs);
            }
        }
    }

    public static void FireAndForget<T>(Func<Task<T>> asyncAction, ILogger logger = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await asyncAction();
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Failed when fire and forget at {asyncAction.GetName()}");
            }
        });
    }

    public static void FireAndForget(Func<Task> asyncAction, ILogger logger = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await asyncAction();
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Failed when fire and forget at {asyncAction.GetName()}");
            }
        });
    }
}
