using System.Text.RegularExpressions;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Audit.Registration;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Defaults;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.OpenTelemetry.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Sample.ModularMonolith;

/// <summary>
/// Builds the modular monolith sample host used by tests, docs, and local exploration.
/// </summary>
public static class ModularMonolithSampleApp
{
    /// <summary>
    /// Builds the modular monolith sample application with the default Cephalon host wiring.
    /// </summary>
    /// <param name="args">
    /// Optional command-line arguments for the sample host.
    /// </param>
    /// <param name="configureBuilder">
    /// Optional hook that can customize the application builder before the host is built.
    /// </param>
    /// <returns>
    /// The configured modular monolith sample application.
    /// </returns>
    public static WebApplication Build(
        string[]? args = null,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        args ??= [];
        var contentRoot = AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(ModularMonolithSampleApp).Assembly.FullName,
            ContentRootPath = contentRoot,
            EnvironmentName = ResolveEnvironmentName(args)
        });

        builder.Configuration.AddJsonFile("modular-monolith.settings.json", optional: false, reloadOnChange: false);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);

        configureBuilder?.Invoke(builder);

        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(behaviors => behaviors.AddHttpBehaviorBindings());
            engine.AddSfidIds();
            engine.AddAudit();
        });

        // Demonstrate the canonical Cephalon.Diagnostics redaction recipe (see
        // docs/components/diagnostics.md "Redaction quick start"). Filters apply at the
        // engine boundary so HTTP and engine-runtime tag values are scrubbed before
        // exporter dispatch. AddCephalon already registered the empty pipeline; these
        // singletons populate it.
        builder.Services.AddSingleton<IRedactionFilter>(new KeyMatchRedactionFilter(
        [
            "http.request.header.authorization",
            "http.request.header.cookie",
            "http.request.header.proxy-authorization",
            "http.response.header.set-cookie",
            "cephalon.tenant.secret",
        ]));
        builder.Services.AddSingleton<IRedactionFilter>(new RegexRedactionFilter(
            new Regex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled)));
        builder.Services.AddSingleton<IRedactionFilter>(new RegexRedactionFilter(
            new Regex(@"Bearer\s+[A-Za-z0-9\-_\.]+", RegexOptions.Compiled),
            replacement: "Bearer [REDACTED]"));

        builder.Services.AddCephalonObservability(builder.Configuration);
        builder.AddCephalonOpenTelemetry();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGet("/", () => TypedResults.Ok(new
        {
            sample = "ModularMonolith",
            blueprint = "ModularMonolith"
        })).ExcludeFromDescription();
        app.MapCephalon();

        return app;
    }

    private static string ResolveEnvironmentName(string[] args)
    {
        return FirstNonEmpty(
                TryGetCommandLineArgument(args, "--environment"),
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            ?? Environments.Development;
    }

    private static string? TryGetCommandLineArgument(string[] args, string argumentName)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentName);

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (string.IsNullOrWhiteSpace(argument))
            {
                continue;
            }

            if (argument.StartsWith($"{argumentName}=", StringComparison.OrdinalIgnoreCase))
            {
                return argument[(argumentName.Length + 1)..];
            }

            if (string.Equals(argument, argumentName, StringComparison.OrdinalIgnoreCase) &&
                index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
