var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var quota = new QuotaState(10_000, TimeSpan.FromHours(6));

app.MapGet("/v1/items/{id:int}", (int id, HttpContext context) =>
{
    return quota.Apply(context.Response)
        ? Results.Ok(new { id, name = $"Item {id}" })
        : Results.StatusCode(StatusCodes.Status429TooManyRequests);
});

app.MapPost("/v1/search", (HttpContext context) =>
{
    return quota.Apply(context.Response)
        ? Results.Ok(new { results = Random.Shared.Next(3, 15) })
        : Results.StatusCode(StatusCodes.Status429TooManyRequests);
});

app.MapGet("/v1/account", (HttpContext context) =>
{
    return quota.Apply(context.Response)
        ? Results.Ok(new { account = "demo" })
        : Results.StatusCode(StatusCodes.Status429TooManyRequests);
});

app.MapPost("/v1/webhooks", (HttpContext context) =>
{
    return quota.Apply(context.Response)
        ? Results.Accepted(value: new { accepted = true })
        : Results.StatusCode(StatusCodes.Status429TooManyRequests);
});

app.Run();

internal sealed class QuotaState
{
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly object _gate = new();
    private int _remaining;
    private DateTimeOffset _resetAtUtc;

    public QuotaState(int limit, TimeSpan window)
    {
        _limit = limit;
        _window = window;
        _remaining = limit;
        _resetAtUtc = DateTimeOffset.UtcNow.Add(window);
    }

    public bool Apply(HttpResponse response)
    {
        int remaining;
        DateTimeOffset resetAtUtc;
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (now >= _resetAtUtc)
            {
                _remaining = _limit;
                _resetAtUtc = now.Add(_window);
            }

            if (_remaining > 0)
            {
                _remaining--;
            }

            remaining = _remaining;
            resetAtUtc = _resetAtUtc;
        }

        response.Headers["X-RateLimit-Limit"] = _limit.ToString();
        response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = resetAtUtc.ToUnixTimeSeconds().ToString();
        return remaining > 0;
    }
}
