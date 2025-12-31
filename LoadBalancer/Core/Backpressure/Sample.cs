using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadBalancer.Core.Backpressure;

internal class Sample
{
    public void run()
    {
        //var cts = new CancellationTokenSource();
        //var queue = new BackpressureQueue(10_000);

        //_ = Task.Run(() => queue.RunAsync(cts.Token));

        //Task<byte[]> Handle(byte[] payload)
        //{
        //    var tcs = new TaskCompletionSource<byte[]>(
        //        TaskCreationOptions.RunContinuationsAsynchronously);

        //    if (!queue.TryEnqueue(async () =>
        //    {
        //        try
        //        {
        //            var result = await router.RouteAsync(payload, cts.Token);
        //            tcs.SetResult(result);
        //        }
        //        catch (Exception ex)
        //        {
        //            tcs.SetException(ex);
        //        }
        //    }))
        //    {
        //        throw new Exception("System overloaded");
        //    }

        //    return tcs.Task;
        //}


    }
}
