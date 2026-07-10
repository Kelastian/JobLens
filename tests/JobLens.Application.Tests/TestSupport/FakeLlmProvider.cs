using JobLens.Domain.Abstractions;

namespace JobLens.Application.Tests.TestSupport;

/// <summary>
/// A hand-written test double for ILlmProvider — no mocking framework is
/// installed in this project, and three small interfaces with a handful of
/// methods don't need one. Returns pre-configured responses keyed by the
/// requested type T, and records every prompt it was called with so tests
/// can assert on what AnalysisService actually asked for.
/// </summary>
internal sealed class FakeLlmProvider : ILlmProvider
{
    private readonly Dictionary<Type, object> _responses = new();
    public List<string> ReceivedPrompts { get; } = [];

    public string Name => "Fake";

    public void SetResponse<T>(T response) where T : class => _responses[typeof(T)] = response;

    public Task<T> CompleteAsync<T>(string prompt, CancellationToken cancellationToken = default)
        where T : class
    {
        ReceivedPrompts.Add(prompt);

        if (!_responses.TryGetValue(typeof(T), out var response))
        {
            throw new InvalidOperationException($"No fake response configured for {typeof(T).Name}.");
        }

        return Task.FromResult((T)response);
    }
}
