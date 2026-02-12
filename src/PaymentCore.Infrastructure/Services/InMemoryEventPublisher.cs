using System.Threading.Channels;
using PaymentCore.Application.Interfaces;
using PaymentCore.Domain.Events;

namespace PaymentCore.Infrastructure.Services;

public class InMemoryEventPublisher : IEventPublisher
{
    private readonly Channel<IDomainEvent> _channel;

    public InMemoryEventPublisher()
    {
        _channel = Channel.CreateUnbounded<IDomainEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        await _channel.Writer.WriteAsync(@event, cancellationToken);
    }

    public ChannelReader<IDomainEvent> GetReader() => _channel.Reader;
}
