using AgenteFinanceiro.Api.Hosting;
using AgenteFinanceiro.Application.Agent;
using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Application.Context;
using AgenteFinanceiro.Application.Sessions;
using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Application.Workflow;
using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Infrastructure.Checkpoints;
using AgenteFinanceiro.Infrastructure.Data;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);
var checkpointRoot = CheckpointDirectory.Ensure(builder.Configuration["Checkpoints:Directory"]);

builder.Services.AddSingleton<IFinancialDataRepository, InMemoryFinancialRepository>();
builder.Services.AddSingleton<ICashFlowCalculator, CashFlowCalculator>();
builder.Services.AddSingleton<AnswerComposer>();
builder.Services.AddSingleton<CashFlowTools>();
builder.Services.AddSingleton<ComparisonTools>();
builder.Services.AddSingleton<ReductionTools>();
builder.Services.AddSingleton<FinancialContextProvider>();
builder.Services.AddSingleton<ValidationExecutor>();
builder.Services.AddSingleton<CalculationExecutor>();
builder.Services.AddSingleton<OpinionExecutor>();
builder.Services.AddSingleton(sp => new CashFlowWorkflowFactory(
    sp.GetRequiredService<ValidationExecutor>(),
    sp.GetRequiredService<CalculationExecutor>(),
    sp.GetRequiredService<OpinionExecutor>(),
    checkpointRoot));
builder.Services.AddSingleton<InMemoryAgentSessionStore>();
builder.Services.AddSingleton<FinancialAgentFactory>();
builder.Services.AddSingleton<AIAgent>(sp => sp.GetRequiredService<FinancialAgentFactory>().Create(sp.GetRequiredService<IChatClient>()));
builder.Services.AddHealthChecks();
A2AHostRegistration.AddServer(builder);

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IChatClient, TestingChatClient>();
}
else
{
    builder.Services.AddSingleton(OpenAIChatClientRegistration.Create(builder.Configuration));
}

var app = builder.Build();
app.MapHealthChecks("/health");
A2AHostRegistration.Map(app);
app.Run();

public partial class Program;

file sealed class TestingChatClient : IChatClient
{
    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "A interpretação não foi concluída. Nenhum número foi criado para esta resposta.")));

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        Empty(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    private static async IAsyncEnumerable<ChatResponseUpdate> Empty([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        yield break;
    }
}
