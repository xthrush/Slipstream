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
                var requestType = serviceType.GetGenericArguments()[0];
                if (_handler.GetType().GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericArguments().Contains(requestType)))
                    return _handler;
                return null;
            }

            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                var typedArray = Array.CreateInstance(elementType, _behaviors.Length);
                for (int i = 0; i < _behaviors.Length; i++)
                    typedArray.SetValue(_behaviors[i], i);
                return typedArray;
            }

            if (serviceType == typeof(IDispatcher))
            {
                return new SlipstreamDispatcher(this);
            }

            return null;
        }
    }

    private record UnregisteredRequest : IRequest<string>;

    private class ShortCircuitBehavior : IPipelineBehavior<TestRequest, int>
    {
        public Task<int> Handle(TestRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<int> next)
            => Task.FromResult(-1); // never calls next
    }

    private class TokenCapturingHandler : IRequestHandler<TestRequest, int>
    {
        public CancellationToken CapturedToken { get; private set; }

        public Task<int> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            return Task.FromResult(request.Value);
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

    [Fact]
    public async Task Send_Returns_Correct_Result_Without_Behaviors()
    {
        var handler = new TestHandler();
        var provider = new SimpleProvider(handler);
        var dispatcher = new SlipstreamDispatcher(provider);

        var result = await dispatcher.Send<int>(new TestRequest(5));

        Assert.Equal(10, result);
    }

    [Fact]
    public async Task Send_Throws_When_Request_Is_Null()
    {
        var provider = new SimpleProvider(new TestHandler());
        var dispatcher = new SlipstreamDispatcher(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.Send<int>(null!));
    }

    [Fact]
    public async Task Send_Throws_When_No_Handler_Registered()
    {
        var provider = new SimpleProvider(new TestHandler());
        var dispatcher = new SlipstreamDispatcher(provider);

        // Use a different request type that has no handler
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.Send<string>(new UnregisteredRequest()));
    }

    [Fact]
    public async Task Behavior_Can_Short_Circuit()
    {
        var handler = new TestHandler();
        var provider = new SimpleProvider(handler, new ShortCircuitBehavior());
        var dispatcher = new SlipstreamDispatcher(provider);

        var result = await dispatcher.Send<int>(new TestRequest(5));

        Assert.Equal(-1, result); // handler never called
    }

    [Fact]
    public async Task Pipeline_Order_Log_Is_Correct()
    {
        var log = new List<string>();
        var provider = new SimpleProvider(new TestHandler(),
            new OrderBehavior(log, "one"),
            new OrderBehavior(log, "two"));
        var dispatcher = new SlipstreamDispatcher(provider);

        await dispatcher.Send<int>(new TestRequest(3));

        Assert.Equal(new[] { "one-before", "two-before", "two-after", "one-after" }, log);
    }

    [Fact]
    public async Task CancellationToken_Is_Passed_To_Handler()
    {
        var cts = new CancellationTokenSource();
        var handler = new TokenCapturingHandler();
        var provider = new SimpleProvider(handler);
        var dispatcher = new SlipstreamDispatcher(provider);

        await dispatcher.Send<int>(new TestRequest(1), cts.Token);

        Assert.Equal(cts.Token, handler.CapturedToken);
    }
}
