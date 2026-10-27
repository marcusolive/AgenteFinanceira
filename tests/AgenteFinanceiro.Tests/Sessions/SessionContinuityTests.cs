using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Application.Context;
using AgenteFinanceiro.Application.Sessions;
using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Application.Workflow;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Domain.Finance;
using AgenteFinanceiro.Infrastructure.Data;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgenteFinanceiro.Tests.Sessions;

public class SessionContinuityTests
{
    [Fact]
    public async Task Acompanhamento_no_mesmo_contexto_reusa_janeiro()
    {
        var (workflow, store, agent) = Create();
        var first = await store.GetOrCreateAsync(agent, "aurora-a", CancellationToken.None);
        await workflow.RunAsync("Qual o fluxo de caixa de janeiro de 2026?", first.Conversation, CancellationToken.None);
        var follow = await workflow.RunAsync("compare Folha e Marketing", first.Conversation, CancellationToken.None);
        Assert.Contains("40000.00", follow.Text);
        Assert.Contains("5000.00", follow.Text);
        Assert.Contains("35000.00", follow.Text);
        Assert.Contains("2026-01", follow.Text);
    }

    [Fact]
    public async Task Outro_contexto_nao_herda_o_mes()
    {
        var (workflow, store, agent) = Create();
        var first = await store.GetOrCreateAsync(agent, "aurora-a", CancellationToken.None);
        await workflow.RunAsync("Qual o fluxo de caixa de janeiro de 2026?", first.Conversation, CancellationToken.None);
        var other = await store.GetOrCreateAsync(agent, "aurora-b", CancellationToken.None);
        var follow = await workflow.RunAsync("compare Folha e Marketing", other.Conversation, CancellationToken.None);
        Assert.Contains("Informe o mês", follow.Text);
        Assert.False(store.TryGet("desconhecida", out _));
    }

    [Fact]
    public async Task Limpar_o_store_descarta_a_conversa_sem_gravar_arquivo_de_sessao()
    {
        var root = Path.Combine(Path.GetTempPath(), "agente-session-" + Guid.NewGuid().ToString("N"));
        var (workflow, store, agent) = Create(root);
        var entry = await store.GetOrCreateAsync(agent, "aurora-a", CancellationToken.None);
        await workflow.RunAsync("Qual o fluxo de caixa de janeiro de 2026?", entry.Conversation, CancellationToken.None);
        store.Clear();
        var renewed = await store.GetOrCreateAsync(agent, "aurora-a", CancellationToken.None);
        Assert.Null(renewed.Conversation.EstablishedPeriod);
        Assert.False(Directory.Exists(root) && Directory.EnumerateFiles(root, "*session*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public void Contexto_do_provedor_nao_traz_quantia_nem_credencial()
    {
        var text = FinancialContextProvider.Catalog;
        Assert.Contains("Aurora", text);
        Assert.Contains("2026-01", text);
        Assert.Contains("Marketing", text);
        Assert.DoesNotContain("80000.00", text);
        Assert.DoesNotContain("OpenAI:ApiKey", text);
    }

    private static (CashFlowWorkflowFactory Workflow, InMemoryAgentSessionStore Store, AIAgent Agent) Create(string? root = null)
    {
        var repository = new InMemoryFinancialRepository();
        var calculator = new CashFlowCalculator(repository);
        var composer = new AnswerComposer(repository, calculator);
        var workflow = new CashFlowWorkflowFactory(
            new ValidationExecutor(),
            new CalculationExecutor(calculator),
            new OpinionExecutor(composer),
            root ?? Path.Combine(Path.GetTempPath(), "agente-unused"));
        var store = new InMemoryAgentSessionStore();
        var factory = new AgenteFinanceiro.Application.Agent.FinancialAgentFactory(
            workflow,
            store,
            new CashFlowTools(composer, calculator),
            new ComparisonTools(composer),
            new ReductionTools(calculator),
            new FinancialContextProvider());
        return (workflow, store, factory.Create(new StubChatClient()));
    }

    private sealed class StubChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "substituto")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Empty();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }

        private static async IAsyncEnumerable<ChatResponseUpdate> Empty()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
