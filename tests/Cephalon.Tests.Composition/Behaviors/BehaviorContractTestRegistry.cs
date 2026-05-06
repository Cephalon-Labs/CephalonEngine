using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Behaviors;

internal static class BehaviorContractTestRegistry
{
    private static int registered;

    [ModuleInitializer]
    internal static void Initialize() => EnsureRegistered();

    internal static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref registered, 1) == 1)
        {
            return;
        }

        var assembly = typeof(BehaviorContractTestRegistry).Assembly;
        var contracts = assembly
            .GetTypes()
            .Select(ExtractBehaviorContract)
            .OfType<BehaviorContractDescriptor>()
            .ToArray();

        BehaviorContractRegistry.Register(assembly, contracts);
    }

    private static BehaviorContractDescriptor? ExtractBehaviorContract(Type behaviorType)
    {
        var behavior = behaviorType.GetCustomAttribute<AppBehaviorAttribute>(inherit: false);
        if (behavior is null)
        {
            return null;
        }

        var behaviorInterface = behaviorType
            .GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>));
        if (behaviorInterface is null)
        {
            return null;
        }

        var genericArguments = behaviorInterface.GetGenericArguments();
        var inputType = genericArguments[0];
        var outputType = genericArguments[1];
        var returnsStructuredResult = TryResolveStructuredResultPayloadType(outputType, out var responseType);
        var inputIsScalar = IsScalarInputType(inputType);

        return new BehaviorContractDescriptor(
            behavior.Id,
            behaviorType,
            inputType,
            outputType,
            responseType,
            returnsStructuredResult,
            inputIsScalar,
            inputIsScalar
                ? []
                : inputType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(static property => property.GetMethod is not null)
                    .Select(static property => new BehaviorInputPropertyDescriptor(
                        property.Name,
                        property.PropertyType))
                    .ToArray());
    }

    private static bool TryResolveStructuredResultPayloadType(Type outputType, out Type responseType)
    {
        var effectiveOutputType = Nullable.GetUnderlyingType(outputType) ?? outputType;
        responseType = effectiveOutputType;

        if (!effectiveOutputType.IsGenericType)
        {
            return false;
        }

        var genericDefinition = effectiveOutputType.GetGenericTypeDefinition();
        if (genericDefinition != typeof(Result<>) &&
            genericDefinition != typeof(BehaviorResult<>))
        {
            return false;
        }

        responseType = effectiveOutputType.GetGenericArguments()[0];
        return true;
    }

    private static bool IsScalarInputType(Type inputType)
    {
        var type = Nullable.GetUnderlyingType(inputType) ?? inputType;
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Guid) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly);
    }
}
