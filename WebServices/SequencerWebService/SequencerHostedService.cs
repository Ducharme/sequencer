using SS=SequencerService;

public class SequencerHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;

    public SequencerHostedService(IServiceProvider sp)
    {
        _sp = sp;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Because next call is not awaited, execution of the method continues after the call is completed
        #pragma warning disable CS4014
        SS.Program.Run(_sp);
        #pragma warning restore CS4014
        return Task.CompletedTask;
    }
}
