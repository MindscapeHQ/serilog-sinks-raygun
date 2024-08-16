namespace Serilog.Sinks.Raygun.SampleServiceApp;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            
            _logger.LogError(new Exception("This is an exception"), "This is an error message");

            await Task.Delay(1000, stoppingToken);
        }
    }
}