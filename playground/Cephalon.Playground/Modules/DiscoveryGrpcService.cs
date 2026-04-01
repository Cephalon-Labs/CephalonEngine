using Cephalon.AspNetCore.Grpc.Contracts.Discovery;
using Cephalon.Playground.Services;
using Grpc.Core;

namespace Cephalon.Playground.Modules;

public sealed class DiscoveryGrpcService : DiscoveryService.DiscoveryServiceBase
{
    private readonly GreetingComposer composer;

    public DiscoveryGrpcService(GreetingComposer composer)
    {
        this.composer = composer;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var greeting = composer.Compose(request.Name);
        return Task.FromResult(ToReply(greeting));
    }

    public override async Task StreamPrinciples(
        PrinciplesRequest request,
        IServerStreamWriter<PrincipleReply> responseStream,
        ServerCallContext context)
    {
        foreach (var principle in DiscoveryDefaults.Principles)
        {
            await responseStream.WriteAsync(new PrincipleReply
            {
                Principle = principle
            });
        }
    }

    public override async Task ExchangeGreetings(
        IAsyncStreamReader<HelloRequest> requestStream,
        IServerStreamWriter<HelloReply> responseStream,
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(ToReply(composer.Compose(request.Name)));
        }
    }

    private static HelloReply ToReply(GreetingEnvelope greeting)
    {
        var reply = new HelloReply
        {
            Message = greeting.Message,
            GeneratedAtUtc = greeting.GeneratedAtUtc.ToString("O")
        };

        reply.Traits.AddRange(greeting.Traits);
        return reply;
    }
}
