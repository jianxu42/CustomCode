public abstract class ScriptBase
{
    // Context object
    public IScriptContext Context { get; }

    // CancellationToken for the execution
    public CancellationToken CancellationToken { get; }

    // Helper: Creates a StringContent object from the serialized JSON
    public static StringContent CreateJsonContent(string serializedJson);

    // Abstract method for your code
    public abstract Task<HttpResponseMessage> ExecuteAsync();
}

public interface IScriptContext
{
    // Correlation Id
    string CorrelationId { get; }

    // Connector Operation Id
    string OperationId { get; }

    // Incoming request
    HttpRequestMessage Request { get; }

    // Logger instance
    ILogger Logger { get; }

    // Used to send an HTTP request
    // Use this method to send requests instead of HttpClient.SendAsync
    Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken);
}

public class Script : ScriptBase
{
    public Script(IScriptContext context, CancellationToken cancellationToken)
        : base(context, cancellationToken)
    {
    }

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Create a new HTTP request
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com");

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
        // Use HttpClient to send the request
        using var client = new HttpClient();
        return await client.SendAsync(request, cancellationToken);
    }
}

public class MockLogger : ILogger
{
    // Implement the ILogger methods here
}

public static void Main()
{
    // Create a mock context
    var context = new MockScriptContext();

    // Create a cancellation token
    var cancellationToken = new CancellationToken();

    // Create an instance of your script
    var script = new Script(context, cancellationToken);

    // Run your script
    var response = script.ExecuteAsync().Result;

    // Print the response
    Console.WriteLine(response);
}
