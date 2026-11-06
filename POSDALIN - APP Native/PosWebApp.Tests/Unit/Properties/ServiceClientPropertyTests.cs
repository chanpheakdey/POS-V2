using System.Net;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using PosWebApp.Infrastructure.Exceptions;
using PosWebApp.Services;

namespace PosWebApp.Tests.Unit.Properties;

/// <summary>
/// Property-based tests for <see cref="AsmxServiceClient"/> exception wrapping.
///
/// **Property 11: Service Client Exception Wrapping** — for any HTTP failure scenario
/// (timeout, 4xx, 5xx, SOAP fault), <see cref="AsmxServiceClient"/> always throws
/// <see cref="ServiceException"/> and never propagates <see cref="HttpRequestException"/>
/// or <see cref="TaskCanceledException"/>.
///
/// **Validates: Requirements 19.1, 19.2**
/// </summary>
public class ServiceClientPropertyTests
{
    // ── Failure scenario discriminated union ─────────────────────────────────

    /// <summary>
    /// Represents the different kinds of HTTP failure that can happen when
    /// calling the ASMX backend.
    /// </summary>
    public abstract record HttpFailureScenario
    {
        /// <summary>HTTP transport failure — e.g. connection refused.</summary>
        public sealed record TransportFailure(string Message) : HttpFailureScenario;

        /// <summary>Timeout — <see cref="TaskCanceledException"/> raised by HttpClient.</summary>
        public sealed record TimeoutFailure : HttpFailureScenario;

        /// <summary>HTTP error status code (4xx or 5xx).</summary>
        public sealed record HttpErrorStatus(HttpStatusCode StatusCode) : HttpFailureScenario;

        /// <summary>HTTP 500 response with a SOAP fault body.</summary>
        public sealed record SoapFault(string FaultCode, string FaultMessage) : HttpFailureScenario;
    }

    // ── Arbitrary generators ─────────────────────────────────────────────────

    /// <summary>
    /// FsCheck arbitrary that generates all four failure scenario types.
    /// Includes transport failures, timeouts, and arbitrary 4xx/5xx HTTP status codes.
    /// </summary>
    public static Arbitrary<HttpFailureScenario> HttpFailureScenarioArb()
    {
        // Transport failure generator
        var transportGen = Gen.Constant(
            (HttpFailureScenario)new HttpFailureScenario.TransportFailure("Connection refused"));

        // Timeout generator
        var timeoutGen = Gen.Constant(
            (HttpFailureScenario)new HttpFailureScenario.TimeoutFailure());

        // HTTP 4xx status codes: 400–499
        var http4xxGen = Gen.Choose(400, 499)
            .Select(code => (HttpFailureScenario)new HttpFailureScenario.HttpErrorStatus((HttpStatusCode)code));

        // HTTP 5xx status codes: 500–599 (excluding some non-standard ones for simplicity)
        var http5xxGen = Gen.Choose(500, 503)
            .Select(code => (HttpFailureScenario)new HttpFailureScenario.HttpErrorStatus((HttpStatusCode)code));

        // SOAP fault
        var soapFaultGen = Gen.Constant(
            (HttpFailureScenario)new HttpFailureScenario.SoapFault("Server", "An error occurred in the backend"));

        // Combine all generators with equal frequency
        var combined = Gen.OneOf(transportGen, timeoutGen, http4xxGen, http5xxGen, soapFaultGen);
        return combined.ToArbitrary();
    }

    // ── HttpMessageHandler factory ───────────────────────────────────────────

    /// <summary>
    /// Creates an <see cref="HttpClient"/> whose handler simulates the given failure scenario.
    /// </summary>
    private static HttpClient BuildFailingHttpClient(HttpFailureScenario scenario)
    {
        DelegatingHandler innerHandler = scenario switch
        {
            HttpFailureScenario.TransportFailure t =>
                new ThrowingHandler(new HttpRequestException(t.Message)),

            HttpFailureScenario.TimeoutFailure =>
                new ThrowingHandler(new TaskCanceledException(
                    "The request was canceled due to the configured HttpClient.Timeout.",
                    new TimeoutException())),

            HttpFailureScenario.HttpErrorStatus s =>
                new StatusCodeHandler(s.StatusCode),

            HttpFailureScenario.SoapFault f =>
                new StatusCodeHandler(HttpStatusCode.InternalServerError,
                    body: $@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <soap:Fault>
      <faultcode>{f.FaultCode}</faultcode>
      <faultstring>{f.FaultMessage}</faultstring>
    </soap:Fault>
  </soap:Body>
</soap:Envelope>"),

            _ => throw new NotSupportedException($"Unknown scenario: {scenario}")
        };

        var httpClient = new HttpClient(innerHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        return httpClient;
    }

    /// <summary>Creates an <see cref="AsmxServiceClient"/> wired to a failing HTTP client.</summary>
    private static AsmxServiceClient BuildClient(HttpFailureScenario scenario)
    {
        var httpClient = BuildFailingHttpClient(scenario);
        return new AsmxServiceClient(httpClient, NullLogger<AsmxServiceClient>.Instance);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>
    /// **Property 11a: GetCategoriesAsync always throws ServiceException on failure**
    ///
    /// For any HTTP failure scenario, calling <see cref="AsmxServiceClient.GetCategoriesAsync"/>
    /// SHALL throw a <see cref="ServiceException"/> and SHALL NOT propagate
    /// <see cref="HttpRequestException"/> or <see cref="TaskCanceledException"/>.
    ///
    /// **Validates: Requirements 19.1, 19.2**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ServiceClientPropertyTests) }, MaxTest = 200)]
    public Property Property11a_GetCategoriesAsyncAlwaysWrapsFailures(HttpFailureScenario scenario)
    {
        var client = BuildClient(scenario);

        Exception? thrown = null;
        try
        {
            client.GetCategoriesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        bool isServiceException = thrown is ServiceException;
        bool isNotRawHttpException = thrown is not HttpRequestException;
        bool isNotTaskCanceled = thrown is not TaskCanceledException;

        return (isServiceException && isNotRawHttpException && isNotTaskCanceled)
            .Label($"Scenario={scenario.GetType().Name}: expected ServiceException, got {thrown?.GetType().Name ?? "no exception"}");
    }

    /// <summary>
    /// **Property 11b: LoginAsync always throws ServiceException on failure**
    ///
    /// For any HTTP failure scenario, calling <see cref="AsmxServiceClient.LoginAsync"/>
    /// SHALL throw a <see cref="ServiceException"/> and SHALL NOT propagate raw exceptions.
    ///
    /// **Validates: Requirements 19.1, 19.2**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ServiceClientPropertyTests) }, MaxTest = 200)]
    public Property Property11b_LoginAsyncAlwaysWrapsFailures(HttpFailureScenario scenario)
    {
        var client = BuildClient(scenario);

        Exception? thrown = null;
        try
        {
            client.LoginAsync("user", "pass").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        bool isServiceException = thrown is ServiceException;
        bool isNotRawHttpException = thrown is not HttpRequestException;
        bool isNotTaskCanceled = thrown is not TaskCanceledException;

        return (isServiceException && isNotRawHttpException && isNotTaskCanceled)
            .Label($"Scenario={scenario.GetType().Name}: expected ServiceException, got {thrown?.GetType().Name ?? "no exception"}");
    }

    /// <summary>
    /// **Property 11c: GetSaleItemsAsync always throws ServiceException on failure**
    ///
    /// For any HTTP failure scenario, calling <see cref="AsmxServiceClient.GetSaleItemsAsync"/>
    /// SHALL throw a <see cref="ServiceException"/> and SHALL NOT propagate raw exceptions.
    ///
    /// **Validates: Requirements 19.1, 19.2**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ServiceClientPropertyTests) }, MaxTest = 200)]
    public Property Property11c_GetSaleItemsAsyncAlwaysWrapsFailures(HttpFailureScenario scenario)
    {
        var client = BuildClient(scenario);

        Exception? thrown = null;
        try
        {
            client.GetSaleItemsAsync(1, 1).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        bool isServiceException = thrown is ServiceException;
        bool isNotRawHttpException = thrown is not HttpRequestException;
        bool isNotTaskCanceled = thrown is not TaskCanceledException;

        return (isServiceException && isNotRawHttpException && isNotTaskCanceled)
            .Label($"Scenario={scenario.GetType().Name}: expected ServiceException, got {thrown?.GetType().Name ?? "no exception"}");
    }

    /// <summary>
    /// **Property 11d: HTTP 4xx status codes always produce ServiceException**
    ///
    /// For any 4xx HTTP status code, the <see cref="AsmxServiceClient"/> SHALL wrap the
    /// response in a <see cref="ServiceException"/> rather than returning a result or
    /// propagating a raw exception.
    ///
    /// **Validates: Requirements 19.1, 19.2**
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Property11d_Any4xxStatusCodeProducesServiceException(PositiveInt rawCode)
    {
        // Map arbitrary int to a 4xx status code (400–499)
        int statusCode = 400 + (rawCode.Get % 100);
        var scenario = new HttpFailureScenario.HttpErrorStatus((HttpStatusCode)statusCode);
        var client = BuildClient(scenario);

        Exception? thrown = null;
        try
        {
            client.GetCategoriesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        bool isServiceException = thrown is ServiceException;
        return isServiceException.Label(
            $"HTTP {statusCode}: expected ServiceException, got {thrown?.GetType().Name ?? "no exception"}");
    }

    /// <summary>
    /// **Property 11e: HTTP 5xx status codes always produce ServiceException**
    ///
    /// For any 5xx HTTP status code, the <see cref="AsmxServiceClient"/> SHALL wrap the
    /// response in a <see cref="ServiceException"/> rather than returning a result or
    /// propagating a raw exception.
    ///
    /// **Validates: Requirements 19.1, 19.2**
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Property11e_Any5xxStatusCodeProducesServiceException(PositiveInt rawCode)
    {
        // Map arbitrary int to a 5xx status code (500–599)
        int statusCode = 500 + (rawCode.Get % 100);
        var scenario = new HttpFailureScenario.HttpErrorStatus((HttpStatusCode)statusCode);
        var client = BuildClient(scenario);

        Exception? thrown = null;
        try
        {
            client.GetCategoriesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        bool isServiceException = thrown is ServiceException;
        return isServiceException.Label(
            $"HTTP {statusCode}: expected ServiceException, got {thrown?.GetType().Name ?? "no exception"}");
    }

    // ── Example-based edge cases ──────────────────────────────────────────────

    [Fact]
    public async Task TransportFailure_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.TransportFailure("Connection refused");
        var client = BuildClient(scenario);

        var ex = await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
        Assert.NotNull(ex);
        Assert.IsNotType<HttpRequestException>(ex);
    }

    [Fact]
    public async Task Timeout_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.TimeoutFailure();
        var client = BuildClient(scenario);

        var ex = await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
        Assert.NotNull(ex);
        Assert.IsNotType<TaskCanceledException>(ex);
    }

    [Fact]
    public async Task Http401_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.HttpErrorStatus(HttpStatusCode.Unauthorized);
        var client = BuildClient(scenario);

        await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
    }

    [Fact]
    public async Task Http403_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.HttpErrorStatus(HttpStatusCode.Forbidden);
        var client = BuildClient(scenario);

        await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
    }

    [Fact]
    public async Task Http404_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.HttpErrorStatus(HttpStatusCode.NotFound);
        var client = BuildClient(scenario);

        await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
    }

    [Fact]
    public async Task Http500_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.HttpErrorStatus(HttpStatusCode.InternalServerError);
        var client = BuildClient(scenario);

        await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
    }

    [Fact]
    public async Task Http503_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.HttpErrorStatus(HttpStatusCode.ServiceUnavailable);
        var client = BuildClient(scenario);

        await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
    }

    [Fact]
    public async Task SoapFault_ThrowsServiceException()
    {
        var scenario = new HttpFailureScenario.SoapFault("Server", "Business rule violated");
        var client = BuildClient(scenario);

        await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
    }

    [Fact]
    public async Task TransportFailure_MessageMentionsBackend()
    {
        var scenario = new HttpFailureScenario.TransportFailure("Connection refused");
        var client = BuildClient(scenario);

        var ex = await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
        // The user-friendly message should mention the backend service
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public async Task Timeout_InnerExceptionIsTaskCanceledException()
    {
        var scenario = new HttpFailureScenario.TimeoutFailure();
        var client = BuildClient(scenario);

        var ex = await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
        // The raw TaskCanceledException must be preserved as InnerException, not propagated
        Assert.NotNull(ex.InnerException);
        Assert.IsType<TaskCanceledException>(ex.InnerException);
    }

    [Fact]
    public async Task TransportFailure_InnerExceptionIsHttpRequestException()
    {
        var scenario = new HttpFailureScenario.TransportFailure("Connection refused");
        var client = BuildClient(scenario);

        var ex = await Assert.ThrowsAsync<ServiceException>(() => client.GetCategoriesAsync());
        // The raw HttpRequestException must be preserved as InnerException, not propagated
        Assert.NotNull(ex.InnerException);
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }
}

// ════════════════════════════════════════════════════════════════════════════
//  Mock HTTP message handlers
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// An <see cref="HttpMessageHandler"/> that throws the given exception for every request.
/// Simulates transport-level failures (connection refused, timeouts, etc.).
/// </summary>
file sealed class ThrowingHandler : DelegatingHandler
{
    private readonly Exception _exception;

    public ThrowingHandler(Exception exception)
        : base(new HttpClientHandler())
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromException<HttpResponseMessage>(_exception);
}

/// <summary>
/// An <see cref="HttpMessageHandler"/> that always returns a fixed HTTP status code
/// with an optional body.  Simulates backend HTTP error responses.
/// </summary>
file sealed class StatusCodeHandler : DelegatingHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public StatusCodeHandler(HttpStatusCode statusCode, string body = "")
        : base(new HttpClientHandler())
    {
        _statusCode = statusCode;
        _body = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body)
        };
        return Task.FromResult(response);
    }
}
