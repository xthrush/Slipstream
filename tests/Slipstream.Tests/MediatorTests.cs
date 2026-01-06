using Slipstream.Abstractions;

namespace Slipstream.Tests;

public class MediatorTests
{
    private record TestRequest(int Value) : IRequest<int>;

    private class TestHandler : IRequestHandler<TestRequest, int>
    {
        public Task<int> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request.Value * 2);
        }
    }

    private class OrderBehavior : IPipelineBehavior<TestRequest, int>
    {
        private readonly IList<string> _log;
        private readonly string _name;
        public OrderBehavior(IList<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public async Task<int> Handle(TestRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<int> next)
        {
            _log.Add($"{_name}-before");
            var r = await next().ConfigureAwait(false);
            _log.Add($"{_name}-after");
            return r;
        }
    }

    private class SimpleProvider : IServiceProvider
    {
        private readonly object _handler;
        private readonly object[] _behaviors;

        public SimpleProvider(object handler, params object[] behaviors)
        {
            _handler = handler;
            _behaviors = behaviors;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            {
                return _handler;
            }

            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return _behaviors;
            }

            if (serviceType == typeof(IDispatcher))
            {
                return new SlipstreamDispatcher(this);
            }

            return null;
        }
    }

    [Fact]
    public async Task Pipeline_Order_Is_Respected()
    {
        var log = new List<string>();
        var handler = new TestHandler();
        var b1 = new OrderBehavior(log, "one");
        var b2 = new OrderBehavior(log, "two");

        var provider = new SimpleProvider(handler, b1, b2);

        var dispatcher = new SlipstreamDispatcher(provider);

        var request = new TestRequest(3);
        var result = await dispatcher.Send<int>(request);

        Assert.Equal(6, result);
        Assert.Equal(4, log.Count); // ensure behaviors executed
    }
}
