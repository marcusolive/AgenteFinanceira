namespace AgenteFinanceiro.Application.Sessions;

public static class ConversationScope
{
    private static readonly AsyncLocal<string?> Current = new();

    public static string ContextId => string.IsNullOrWhiteSpace(Current.Value) ? "sem-contexto" : Current.Value;

    public static IDisposable Push(string contextId)
    {
        var previous = Current.Value;
        Current.Value = contextId;
        return new Pop(previous);
    }

    private sealed class Pop(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
