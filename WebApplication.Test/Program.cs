using LoadBalancer.Core.Backpressure;
using LoadBalancer.Core.LoadBalancing;
using LoadBalancer.Core.Pool;
using LoadBalancer.Core.Routing;
using LoadBalancer.Infrastructure.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

/* ================= CONFIG ================= */

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false);

/* ================= LOAD SETTINGS ================= */

var settings = builder.Configuration
    .GetSection("LoadBalancerSettings")
    .Get<Loadbalancersettings>()!;

/* ================= SERVICES ================= */

builder.Services.AddSingleton(settings);

builder.Services.AddSingleton(provider =>
{
    var pool = new ChannelPool();

    var acq = settings.Acquirers.First(a => a.AcquirerId == "ACQ1");

    pool.Reload(acq.Channels);
    pool.StartHeartbeat(
        TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs),
        TimeSpan.FromMilliseconds(settings.HeartbeatIntervalMs),
        CancellationToken.None);

    return pool;
});

builder.Services.AddSingleton(provider =>
{
    var pool = provider.GetRequiredService<ChannelPool>();
    var acq = settings.Acquirers.First(a => a.AcquirerId == "ACQ1");

    ILoadBalancingStrategy strategy = acq.Strategy switch
    {
        "LeastConnections" => new LeastConnectionsStrategy(pool.Routable),
        "RoundRobin" => new RoundRobinStrategy(pool.Routable),
        _ => new AdaptiveStrategy(pool.Routable)
    };

    return new RoutingEngine(pool, strategy);
});

builder.Services.AddSingleton<IAdaptiveBackpressure>(provider =>
{
    var pool = provider.GetRequiredService<ChannelPool>();

    return new AdaptiveBackpressureQueue(
        baseConcurrency: settings.MaxConcurrent,
        snapshot: pool.CreateSnapshot);
});

/* ================= APP ================= */

var app = builder.Build();

/* ================= ENDPOINT ================= */

app.MapPost("/tx", async (
    HttpContext ctx,
    IAdaptiveBackpressure queue,
    RoutingEngine router) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var payload = Encoding.UTF8.GetBytes(body);

    try
    {
        var res = await queue.EnqueueAsync(
            ct => router.RouteAsync(payload, ct),
            ctx.RequestAborted);

        return Results.Ok(new
        {
            status = "OK",
            length = res.Length
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});


app.MapPost("/trx", async (
    HttpContext ctx) =>
{
    var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
    var payload = Encoding.UTF8.GetBytes(body);

    try
    {
      

        return Results.Ok(new
        {
            status = "OK",
           
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

/* ================= METRICS ================= */

app.MapGet("/metrics", (ChannelPool pool) =>
{
    var data = pool.All().Select(ch => new
    {
        ch.Transport.Name,
        Circuit = ch.Circuit.State.ToString(),
        ch.Metrics.InFlight,
        ch.Metrics.Success,
        ch.Metrics.Failure,
        ch.Metrics.LastLatencyMs
    });

    return Results.Ok(data);
});

app.Run();
