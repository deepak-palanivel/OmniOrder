using System.Threading.Channels;
using OmniOrder.Application.Events;

namespace OmniOrder.Infrastructure.Channels;

public sealed class OrderAuditChannel
{
    private readonly Channel<OrderAuditEvent> _channel;

    public OrderAuditChannel()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<OrderAuditEvent>(options);
    }

    public ChannelWriter<OrderAuditEvent> Writer => _channel.Writer;
    public ChannelReader<OrderAuditEvent> Reader => _channel.Reader;
}
