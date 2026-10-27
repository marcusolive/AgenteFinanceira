using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AgenteFinanceiro.Application.Middleware;

public sealed class ExecutionLogBuffer
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Add(string line) => _lines.Add(line);

    public void Clear() => _lines.Clear();
}

public static class ExecutionLogMiddleware
{
    public const string AgentName = "AgenteFinanceiro";

    public static async Task<AgentResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(innerAgent);
        _ = messages;
        _ = options;
        var watch = Stopwatch.StartNew();
        var outcome = "sucesso";
        string? exceptionType = null;
        try
        {
            return await innerAgent.RunAsync(messages, session, options, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            outcome = "falha";
            exceptionType = exception.GetType().Name;
            throw;
        }
        finally
        {
            watch.Stop();
            Write(session, watch.ElapsedMilliseconds, outcome, exceptionType);
        }
    }

    public static async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var outcome = "sucesso";
        string? exceptionType = null;
        var iterator = innerAgent.RunStreamingAsync(messages, session, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                AgentResponseUpdate update;
                try
                {
                    if (!await iterator.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    update = iterator.Current;
                }
                catch (Exception exception)
                {
                    outcome = "falha";
                    exceptionType = exception.GetType().Name;
                    throw;
                }

                yield return update;
            }
        }
        finally
        {
            await iterator.DisposeAsync().ConfigureAwait(false);
            watch.Stop();
            Write(session, watch.ElapsedMilliseconds, outcome, exceptionType);
        }
    }

    public static void WriteLine(AgentSession? session, long durationMs, string outcome, string? exceptionType) =>
        Write(session, durationMs, outcome, exceptionType);

    private static void Write(AgentSession? session, long durationMs, string outcome, string? exceptionType)
    {
        var sessionId = session is null ? "sem-sessao" : "sessao-presente";
        var line = $"agente={AgentName}; sessao={sessionId}; duracaoMs={durationMs}; desfecho={outcome}; excecao={exceptionType ?? "nenhuma"}";
        ExecutionLog.Buffer.Add(line);
        ExecutionLog.Logger?.LogInformation("{Line}", line);
    }
}

public static class ExecutionLog
{
    public static ExecutionLogBuffer Buffer { get; } = new();

    public static ILogger? Logger { get; set; }
}
