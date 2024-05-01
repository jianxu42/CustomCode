using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // Create a mock context
        var context = new MockScriptContext();

        // Create a cancellation token
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Create an instance of your script
        var script = new Script(context, cancellationToken);

        // Run your script
        var response = await script.ExecuteAsync();

        // Print the response
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}

public abstract class ScriptBase
{
    public IScriptContext Context { get; }
    public CancellationToken CancellationToken { get; }

    protected ScriptBase(IScriptContext context, CancellationToken cancellationToken)
    {
        Context = context;
        CancellationToken = cancellationToken;
    }

    public abstract Task<HttpResponseMessage> ExecuteAsync();
}

public interface IScriptContext
{
    string CorrelationId { get; }
    string OperationId { get; }
    HttpRequestMessage Request { get; }
    ILogger Logger { get; }
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

public class Script : ScriptBase
{
    public Script(IScriptContext context, CancellationToken cancellationToken) : base(context, cancellationToken)
    {
    }

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Create a new HTTP request
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com");

        // Add a User-Agent header to the request
        request.Headers.Add("User-Agent", "Chrome191");

        // Copy headers from the context request to the new request
        foreach (var header in Context.Request.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Use the SendAsync method from the IScriptContext interface to send the request
        var response = await Context.SendAsync(request, CancellationToken);

        return response;
    }
}

public class MockScriptContext : IScriptContext
{
    public string CorrelationId => "test-correlation-id";
    public string OperationId => "test-operation-id";
    public HttpRequestMessage Request => new HttpRequestMessage();
    public ILogger Logger => new MockLogger();

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        return await client.SendAsync(request, cancellationToken);
    }
}

public class MockLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        // To silence the nullable warning, we're returning a dummy IDisposable.
        // This is common in mock or stub implementations where the scope functionality is not needed.
        return new DisposableScope();
    }

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Simple console output to observe the behavior. Adjust the formatter usage as necessary.
        Console.WriteLine(formatter(state, exception));
    }

    private class DisposableScope : IDisposable
    {
        public void Dispose()
        {
            // Nothing to dispose in this mock implementation.
        }
    }
}
