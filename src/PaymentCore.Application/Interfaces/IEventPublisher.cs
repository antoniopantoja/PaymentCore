using PaymentCore.Domain.Events;

namespace PaymentCore.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
