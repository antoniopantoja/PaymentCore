using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentCore.Infrastructure.Services;

namespace PaymentCore.Infrastructure.BackgroundServices;

public class EventProcessorBackgroundService : BackgroundService
{
    private readonly InMemoryEventPublisher _eventPublisher;
    private readonly ILogger<EventProcessorBackgroundService> _logger;

    public EventProcessorBackgroundService(
        InMemoryEventPublisher eventPublisher,
        ILogger<EventProcessorBackgroundService> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Processor Background Service started");

        var reader = _eventPublisher.GetReader();

        await foreach (var @event in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation(
                    "Processing event {EventType} - {EventId}",
                    @event.GetType().Name,
                    @event.EventId);

                // Here you can add custom event processing logic
                // For example: send to message queue, update read models, etc.

                await Task.Delay(10, stoppingToken); // Simulate processing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event {EventId}", @event.EventId);
            }
        }
    }
}
