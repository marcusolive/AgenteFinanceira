using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AgenteFinanceiro.Tests.Integration;

public sealed class AgenteFinanceiroApiFactory : WebApplicationFactory<Program>
{
    public string CheckpointDirectory { get; } = Path.Combine(Path.GetTempPath(), "agente-a2a-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Checkpoints:Directory", CheckpointDirectory);
        builder.ConfigureTestServices(services =>
        {
            var existing = services.Where(descriptor => descriptor.ServiceType == typeof(IChatClient)).ToList();
            foreach (var descriptor in existing)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IChatClient, SubstituteChatClient>();
        });
    }
}

public sealed class SubstituteChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "A interpretação não foi concluída. Nenhum número foi criado para esta resposta.")));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
