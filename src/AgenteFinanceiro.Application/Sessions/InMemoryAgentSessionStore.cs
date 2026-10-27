using System.Collections.Concurrent;
using AgenteFinanceiro.Domain.Finance;
using Microsoft.Agents.AI;

namespace AgenteFinanceiro.Application.Sessions;

public sealed class SessionEntry
{
    public required string ContextId { get; init; }

    public required AgentSession AgentSession { get; init; }

    public required ConversationSession Conversation { get; init; }
}

public sealed class InMemoryAgentSessionStore
{
    private readonly ConcurrentDictionary<string, SessionEntry> _entries = new(StringComparer.Ordinal);

    public async Task<SessionEntry> GetOrCreateAsync(AIAgent agent, string contextId, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(contextId, out var existing))
        {
            return existing;
        }

        var agentSession = await agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        var created = new SessionEntry
        {
            ContextId = contextId,
            AgentSession = agentSession,
            Conversation = new ConversationSession { ContextId = contextId }
        };
        return _entries.GetOrAdd(contextId, created);
    }

    public bool TryGet(string contextId, out SessionEntry? entry) =>
        _entries.TryGetValue(contextId, out entry);

    public void Clear() => _entries.Clear();
}
