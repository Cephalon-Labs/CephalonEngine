using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;

namespace Cephalon.Tests.Hosting;

internal static class BehaviorRestProfileTestRegistry
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

        var assembly = typeof(BehaviorRestProfileTestRegistry).Assembly;
        var contracts = new List<BehaviorContractDescriptor>();
        var profiles = new List<BehaviorRestProfileDescriptor>();
        var behaviorTypes = new List<BehaviorRestProfileBehaviorTypeDescriptor>();

        foreach (var type in assembly.GetTypes())
        {
            var behavior = type.GetCustomAttribute<AppBehaviorAttribute>(inherit: false);
            if (behavior is null)
            {
                continue;
            }

            var contract = ExtractBehaviorContract(type, behavior.Id);
            if (contract is not null)
            {
                contracts.Add(contract);
            }

            var profile = type.GetCustomAttribute<BehaviorRestProfileAttribute>(inherit: false);
            if (profile is null)
            {
                continue;
            }

            profiles.Add(new BehaviorRestProfileDescriptor(
                behavior.Id,
                profile.Method,
                profile.RelativePattern,
                profile.ApiVersionMajor > 0 ? profile.ApiVersionMajor : null,
                ExtractBindings(type),
                profile.PreserveImplicitQueryFallback)
            {
                InputContract = ExtractInputContract(type)
            });
            behaviorTypes.Add(new BehaviorRestProfileBehaviorTypeDescriptor(behavior.Id, type));
        }

        BehaviorContractRegistry.Register(assembly, contracts);
        BehaviorRestGeneratedProfileRegistry.Register(assembly, profiles, behaviorTypes);
    }

    private static BehaviorRestBindingDescriptor[] ExtractBindings(MemberInfo type)
        => type.GetCustomAttributes<BehaviorRestBindingAttribute>(inherit: false)
            .Select(static attribute => new BehaviorRestBindingDescriptor(
                attribute.PropertyName,
                attribute.Source,
                attribute.Name))
            .ToArray();

    private static BehaviorRestInputContractDescriptor? ExtractInputContract(Type behaviorType)
    {
        var behaviorInterface = ResolveBehaviorInterface(behaviorType);
        if (behaviorInterface is null)
        {
            return null;
        }

        var inputType = behaviorInterface.GetGenericArguments()[0];
        return BuildRestInputContract(inputType);
    }

    private static BehaviorContractDescriptor? ExtractBehaviorContract(Type behaviorType, string id)
    {
        var behaviorInterface = ResolveBehaviorInterface(behaviorType);
        if (behaviorInterface is null)
        {
            return null;
        }

        var genericArguments = behaviorInterface.GetGenericArguments();
        var inputType = genericArguments[0];
        var outputType = genericArguments[1];
        var returnsStructuredResult = TryResolveStructuredResultPayloadType(outputType, out var responseType);
        var isScalar = IsScalarInputType(inputType);
        return new BehaviorContractDescriptor(
            id,
            behaviorType,
            inputType,
            outputType,
            responseType,
            returnsStructuredResult,
            isScalar,
            isScalar
                ? []
                : inputType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(static property => property.GetMethod is not null)
                    .Select(static property => new BehaviorInputPropertyDescriptor(
                        property.Name,
                        property.PropertyType))
                    .ToArray());
    }

    private static Type? ResolveBehaviorInterface(Type behaviorType)
        => behaviorType
            .GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>));

    private static BehaviorRestInputContractDescriptor BuildRestInputContract(Type inputType)
    {
        var isScalar = IsScalarInputType(inputType);
        return new BehaviorRestInputContractDescriptor(
            inputType,
            isScalar,
            isScalar
                ? []
                : inputType
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(static property => property.GetMethod is not null)
                    .Select(static property => new BehaviorRestInputPropertyDescriptor(
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
