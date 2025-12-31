
//// See https://aka.ms/new-console-template for more information

//using LoadBalancer.Core.Backpressure;
//using LoadBalancer.Infrastructure.Health;

//var metrics = new MetricsCollector();
//var queue = new BackpressureQueue(capacity: 10_000);
//var cts = new CancellationTokenSource();
////var router = new RoutingEngine();

//_ = Task.Run(() => queue.RunAsync(cts.Token));

//async Task<byte[]> HandleRequest(
//    string terminal,
//    string trace,
//    byte[] payload)
//{
//    var tcs = new TaskCompletionSource<byte[]>(
//        TaskCreationOptions.RunContinuationsAsynchronously);

//    if (!queue.TryEnqueue(async () =>
//    {
//        try
//        {
//            var resp = await router.RouteAsync(
//                terminal, trace, payload, cts.Token);
//            tcs.SetResult(resp);
//        }
//        catch (Exception ex)
//        {
//            tcs.SetException(ex);
//        }
//    }))
//    {
//        throw new Exception("System overloaded");
//    }

//    return await tcs.Task;
//}





//Console.WriteLine("Hello, World!");


////builder.Services.Configure<RoutingOptions>(
////    builder.Configuration.GetSection("Routing"));

////builder.Services.Configure<LoadBalancerOptions>(
////    builder.Configuration.GetSection("LoadBalancer"));

////builder.Services.AddSingleton<ChannelPool>();
////builder.Services.AddSingleton<RoutingConfigReloader>();
////builder.Services.AddSingleton<MetricsCollector>();
////builder.Services.AddSingleton<ILoadBalancingStrategy, LeastConnectionsStrategy>();
////builder.Services.AddSingleton<RoutingEngine>();





////builder.Services.AddSingleton<AcquirerRegistry>();
////builder.Services.AddSingleton<AcquirerConfigReloader>();

////builder.Services.AddSingleton<ILoadBalancingStrategy, LeastConnectionsStrategy>();

////builder.Services.AddSingleton<RoutingEngine>();




////builder.Configuration
////    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
///
Console.WriteLine(  "dfdfdf");