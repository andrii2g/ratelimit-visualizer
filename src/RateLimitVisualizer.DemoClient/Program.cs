using System.Diagnostics;
using System.Net.Http.Json;

var options = DemoOptions.Parse(args);
using var client = new HttpClient();

var endpoints = new WeightedEndpoint[]
{
    new("POST", "/v1/search", 55),
    new("GET", "/v1/items/{id}", 30),
    new("POST", "/v1/webhooks", 10),
    new("GET", "/v1/account", 5)
};

for (var i = 1; i <= options.Requests; i++)
{
    var endpoint = PickEndpoint(endpoints);
    var path = endpoint.Path.Replace("{id}", Random.Shared.Next(1, 5000).ToString(), StringComparison.Ordinal);
    var url = $"{options.MockApiUrl.TrimEnd('/')}{path}";
    var stopwatch = Stopwatch.StartNew();
    using var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), url);
    using var response = await client.SendAsync(request);
    stopwatch.Stop();

    var headers = response.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);
    var observation = new
    {
        provider = options.Provider,
        consumer = options.Consumer,
        method = endpoint.Method,
        url,
        statusCode = (int)response.StatusCode,
        latencyMs = (int)stopwatch.ElapsedMilliseconds,
        headers,
        observedAtUtc = DateTimeOffset.UtcNow
    };

    using var visualizerResponse = await client.PostAsJsonAsync($"{options.VisualizerUrl.TrimEnd('/')}/api/observations", observation);
    Console.WriteLine($"{i,5}/{options.Requests} {endpoint.Method} {path} -> {(int)response.StatusCode}, observation {(int)visualizerResponse.StatusCode}");

    if (options.DelayMs > 0)
    {
        await Task.Delay(options.DelayMs);
    }
}

static WeightedEndpoint PickEndpoint(IReadOnlyList<WeightedEndpoint> endpoints)
{
    var roll = Random.Shared.Next(1, endpoints.Sum(endpoint => endpoint.Weight) + 1);
    var total = 0;
    foreach (var endpoint in endpoints)
    {
        total += endpoint.Weight;
        if (roll <= total)
        {
            return endpoint;
        }
    }

    return endpoints[^1];
}

internal sealed record WeightedEndpoint(string Method, string Path, int Weight);

internal sealed record DemoOptions(
    string VisualizerUrl,
    string MockApiUrl,
    string Provider,
    string Consumer,
    int Requests,
    int DelayMs)
{
    public static DemoOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["--visualizer-url"] = "http://localhost:5050",
            ["--mock-api-url"] = "http://localhost:5060",
            ["--provider"] = "demo-api",
            ["--consumer"] = "local-demo-client",
            ["--requests"] = "5000",
            ["--delay-ms"] = "20"
        };

        for (var i = 0; i < args.Length - 1; i += 2)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                values[args[i]] = args[i + 1];
            }
        }

        return new DemoOptions(
            values["--visualizer-url"],
            values["--mock-api-url"],
            values["--provider"],
            values["--consumer"],
            int.Parse(values["--requests"]),
            int.Parse(values["--delay-ms"]));
    }
}
