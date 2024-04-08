using Microsoft.Extensions.Hosting;
using MassTransit;
using BaseSagaDemo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BaseSagaDemo.Demo;

var builder = Host.CreateApplicationBuilder(args);
    
var assembly = typeof(BaseState).Assembly;

builder.Services.AddRabbitMq(builder.Configuration,
    (x) => {
        x.AddConsumers(assembly);
        // By default, sagas are in-memory, but should be changed to a durable
        // saga repository.
        x.SetInMemorySagaRepositoryProvider();

        x.AddConsumers(assembly);
        x.AddSagaStateMachines(assembly);
        x.AddSagas(assembly);
        x.AddActivities(assembly);
    });

builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = true;
        options.StartTimeout = TimeSpan.FromMinutes(1);
        options.StopTimeout = TimeSpan.FromMinutes(1);
    });

builder.Services.AddTransient<SenderClient>();

using IHost host = builder.Build();

Console.WriteLine("Preparing Demo");
try
{
    await host.StartAsync();

    await MakeDemo(host.Services);

    await host.WaitForShutdownAsync();

    Console.WriteLine("Main: RunAsync has completed");
}
finally
{
    Console.WriteLine("Main: stopping");

    if (host is IAsyncDisposable d) await d.DisposeAsync();
}
//await host.RunAsync();

async Task MakeDemo(IServiceProvider hostProvider)
{
    using IServiceScope serviceScope = hostProvider.CreateScope();
    IServiceProvider provider = serviceScope.ServiceProvider;

    Console.WriteLine("Initializing MassTransit Base Saga");

    var obj = provider.GetRequiredService<SenderClient>();
    var data = new TestStart()
    {
        EmpCode = "001",
        CorrelationId = Guid.NewGuid(),
        ActivityTimeUtc = DateTime.UtcNow
    };
    await obj.MakeRequest(data);
    Console.WriteLine("Request sent, now we'll check the state shortly");
    await Task.Delay(3000);

    var client = provider.GetRequiredService<IRequestClient<BaseSagaStateMachineStatus>>();
    try
    {
        Console.WriteLine("Making get status request");
        var response = await client.GetResponse<BaseSagaStateMachineStatusResult>(new BaseSagaStateMachineStatus
        {
            CorrelationId = data.CorrelationId
        });
        Console.WriteLine($"Status is: {response.Message.CurrentState} ({response.Message.CurrentState.GetType().Name})");
    }
    catch (Exception ex) 
    {
        Console.WriteLine($"{ex.Message}");
    }

    Console.WriteLine("Done");

}
public class SenderClient
{
    private readonly IPublishEndpoint _publishEndpoint;
    public SenderClient(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }
    public async Task MakeRequest(TestStart data, CancellationToken cancellation = default)
    {
        await _publishEndpoint.Publish(data, cancellation);
    }
}

public static class Extension
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration
            , Action<IBusRegistrationConfigurator> configure
            , params Type[] addRequestClientTypes)
    {
        var rabbitMq = new RabbitMqOption();
        configuration.GetSection("rabbitmq").Bind(rabbitMq);

        // establish connection with rabbitMQ..
        services.AddMassTransit(x =>
        {
            x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri(rabbitMq.ConnectionString), hostcfg =>
                {
                    hostcfg.Username(rabbitMq.Username);
                    hostcfg.Password(rabbitMq.Password);
                });
                cfg.ConfigureEndpoints(provider);
            }));


            //individual registering the consumer
            if (addRequestClientTypes != null && addRequestClientTypes.Length > 0)
            {
                for (int i = 0; i < addRequestClientTypes.Length; i++)
                {
                    var type = addRequestClientTypes[i];
                    x.AddRequestClient(type);
                }
            }
            configure(x);
        });

        return services;
    }
}

public class RabbitMqOption
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

}