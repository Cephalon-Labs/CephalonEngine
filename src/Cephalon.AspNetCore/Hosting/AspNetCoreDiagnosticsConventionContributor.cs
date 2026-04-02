using Cephalon.Engine.Diagnostics;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AspNetCoreDiagnosticsConventions.Convention;
}

internal static class AspNetCoreDiagnosticsConventions
{
    public const int HttpRequestStartedId = 3200;
    public const int HttpRequestBodyCapturedId = 3201;
    public const int HttpResponseCompletedId = 3202;
    public const int HttpResponseBodyCapturedId = 3203;
    public const int HttpRequestFailedId = 3204;

    public const string HttpRequestStartedName = "HttpRequestStarted";
    public const string HttpRequestBodyCapturedName = "HttpRequestBodyCaptured";
    public const string HttpResponseCompletedName = "HttpResponseCompleted";
    public const string HttpResponseBodyCapturedName = "HttpResponseBodyCaptured";
    public const string HttpRequestFailedName = "HttpRequestFailed";

    public const string HttpRequestStartedMessageTemplate = "HTTP {Method} {Path}{QueryString} started. RequestId {RequestId}. TraceId {TraceId}. SpanId {SpanId}. ContentType {ContentType}. TraceParent {TraceParent}. ContentLength {ContentLength}.";
    public const string HttpRequestBodyCapturedMessageTemplate = "HTTP request body captured for {Method} {Path}{QueryString}. RequestId {RequestId}. TraceId {TraceId}. SpanId {SpanId}. ContentType {ContentType}. Truncated {IsTruncated}. Body {Body}.";
    public const string HttpResponseCompletedMessageTemplate = "HTTP {Method} {Path}{QueryString} completed with {StatusCode} in {ElapsedMilliseconds} ms. RequestId {RequestId}. TraceId {TraceId}. SpanId {SpanId}. ContentType {ContentType}. ContentLength {ContentLength}.";
    public const string HttpResponseBodyCapturedMessageTemplate = "HTTP response body captured for {Method} {Path}{QueryString}. RequestId {RequestId}. TraceId {TraceId}. SpanId {SpanId}. ContentType {ContentType}. Truncated {IsTruncated}. Body {Body}.";
    public const string HttpRequestFailedMessageTemplate = "HTTP {Method} {Path}{QueryString} failed after {ElapsedMilliseconds} ms. RequestId {RequestId}. TraceId {TraceId}. SpanId {SpanId}. StatusCode {StatusCode}.";

    public static readonly DiagnosticEventDefinition HttpRequestStarted = new(
        Id: HttpRequestStartedId,
        Name: HttpRequestStartedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: HttpRequestStartedMessageTemplate,
        Description: "Emitted when Cephalon ASP.NET Core request logging begins tracking one HTTP request.");

    public static readonly DiagnosticEventDefinition HttpRequestBodyCaptured = new(
        Id: HttpRequestBodyCapturedId,
        Name: HttpRequestBodyCapturedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: HttpRequestBodyCapturedMessageTemplate,
        Description: "Emitted when request-body logging is enabled and a textual request body is captured.");

    public static readonly DiagnosticEventDefinition HttpResponseCompleted = new(
        Id: HttpResponseCompletedId,
        Name: HttpResponseCompletedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: HttpResponseCompletedMessageTemplate,
        Description: "Emitted when Cephalon ASP.NET Core request logging completes one HTTP response.");

    public static readonly DiagnosticEventDefinition HttpResponseBodyCaptured = new(
        Id: HttpResponseBodyCapturedId,
        Name: HttpResponseBodyCapturedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: HttpResponseBodyCapturedMessageTemplate,
        Description: "Emitted when response-body logging is enabled and a textual response body is captured.");

    public static readonly DiagnosticEventDefinition HttpRequestFailed = new(
        Id: HttpRequestFailedId,
        Name: HttpRequestFailedName,
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: HttpRequestFailedMessageTemplate,
        Description: "Emitted when the ASP.NET Core pipeline throws before the request completes successfully.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.AspNetCore",
        Description: "Structured HTTP request, response, body-capture, and trace-correlation diagnostics for ASP.NET Core hosts.",
        Events:
        [
            HttpRequestStarted,
            HttpRequestBodyCaptured,
            HttpResponseCompleted,
            HttpResponseBodyCaptured,
            HttpRequestFailed
        ]);
}
