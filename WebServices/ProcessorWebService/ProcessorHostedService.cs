using PS=ProcessorService;

public class ProcessorHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;

    public ProcessorHostedService(IServiceProvider sp)
    {
        _sp = sp;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Because next call is not awaited, execution of the method continues after the call is completed
        #pragma warning disable CS4014
        PS.Program.Run(_sp);
        #pragma warning restore CS4014
        return Task.CompletedTask;
    }
}
