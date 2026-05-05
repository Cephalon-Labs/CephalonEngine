using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Support;

internal sealed class DataDispatchingTestModule : ModuleBase
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "data-dispatching",
        displayName: "Data Dispatching",
        description: "Registers test command and query handlers for Cephalon.Data coverage.",
        tags: ["data", "tests"],
        version: "1.0.0");

    public override ModuleDescriptor Descriptor => DescriptorInstance;

    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DataDispatchingState>();
        services.AddCephalonDataCommand<ActivateTenantCommand>();
        services.AddCephalonDataCommand<CreateOrderCommand, string>();
        services.AddCephalonDataQuery<GetDispatchingSnapshotQuery, DispatchingSnapshot>();
        services.AddSingleton<ICommandHandler<ActivateTenantCommand>, ActivateTenantCommandHandler>();
        services.AddSingleton<ICommandHandler<CreateOrderCommand, string>, CreateOrderCommandHandler>();
        services.AddSingleton<IQueryHandler<GetDispatchingSnapshotQuery, DispatchingSnapshot>, GetDispatchingSnapshotQueryHandler>();
    }

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
    }
}

internal sealed record ActivateTenantCommand(string TenantId) : ICommand;

internal sealed record CreateOrderCommand(string CustomerName) : ICommand<string>;

internal sealed record GetDispatchingSnapshotQuery() : IQuery<DispatchingSnapshot>;

internal sealed record DispatchingSnapshot(int ActivatedTenants, string? LastCreatedOrderId, string? LastCustomerName);

internal sealed class DataDispatchingState
{
    public int ActivatedTenants { get; set; }

    public string? LastCreatedOrderId { get; set; }

    public string? LastCustomerName { get; set; }
}

internal sealed class ActivateTenantCommandHandler(DataDispatchingState state) : ICommandHandler<ActivateTenantCommand>
{
    public ValueTask HandleAsync(ActivateTenantCommand command, CancellationToken cancellationToken = default)
    {
        state.ActivatedTenants++;
        return ValueTask.CompletedTask;
    }
}

internal sealed class CreateOrderCommandHandler(DataDispatchingState state) : ICommandHandler<CreateOrderCommand, string>
{
    public ValueTask<string> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        state.LastCustomerName = command.CustomerName;
        state.LastCreatedOrderId = $"order-{state.ActivatedTenants + 1:000}";
        return ValueTask.FromResult(state.LastCreatedOrderId);
    }
}

internal sealed class GetDispatchingSnapshotQueryHandler(DataDispatchingState state) : IQueryHandler<GetDispatchingSnapshotQuery, DispatchingSnapshot>
{
    public ValueTask<DispatchingSnapshot> HandleAsync(GetDispatchingSnapshotQuery query, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new DispatchingSnapshot(
            state.ActivatedTenants,
            state.LastCreatedOrderId,
            state.LastCustomerName));
    }
}

internal sealed record MissingCommand(string Value) : ICommand;
