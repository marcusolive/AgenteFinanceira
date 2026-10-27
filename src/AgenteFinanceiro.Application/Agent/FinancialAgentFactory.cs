using AgenteFinanceiro.Application.Context;
using AgenteFinanceiro.Application.Middleware;
using AgenteFinanceiro.Application.Sessions;
using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Application.Workflow;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgenteFinanceiro.Application.Agent;

public sealed class FinancialAgentFactory
{
    public const string AgentId = "agente-financeiro";
    public const string AgentName = "AgenteFinanceiro";

    private readonly CashFlowWorkflowFactory _workflow;
    private readonly InMemoryAgentSessionStore _sessions;
    private readonly CashFlowTools _cashFlowTools;
    private readonly ComparisonTools _comparisonTools;
    private readonly ReductionTools _reductionTools;
    private readonly FinancialContextProvider _contextProvider;

    public FinancialAgentFactory(
        CashFlowWorkflowFactory workflow,
        InMemoryAgentSessionStore sessions,
        CashFlowTools cashFlowTools,
        ComparisonTools comparisonTools,
        ReductionTools reductionTools,
        FinancialContextProvider contextProvider)
    {
        _workflow = workflow;
        _sessions = sessions;
        _cashFlowTools = cashFlowTools;
        _comparisonTools = comparisonTools;
        _reductionTools = reductionTools;
        _contextProvider = contextProvider;
    }

    public AIAgent Create(IChatClient chatClient)
    {
        var raw = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Id = AgentId,
                Name = AgentName,
                Description = "Análise de fluxo de caixa da base demonstrativa Aurora.",
                ChatOptions = new ChatOptions
                {
                    Instructions = FinancialAgentInstructions.Text,
                    Tools =
                    [
                        AIFunctionFactory.Create(_cashFlowTools.ObterFluxoCaixaMes),
                        AIFunctionFactory.Create(_cashFlowTools.ListarPeriodosNegativos),
                        AIFunctionFactory.Create(_comparisonTools.CompararCategorias),
                        AIFunctionFactory.Create(_comparisonTools.CompararCategoriaEntreMeses),
                        AIFunctionFactory.Create(_reductionTools.PrepararRecomendacaoReducao)
                    ]
                },
                AIContextProviders = [_contextProvider],
                UseProvidedChatClientAsIs = false
            });

        return raw.AsBuilder()
            .Use(
                (messages, session, options, inner, cancellationToken) =>
                    RunAsync(messages, session, options, inner, cancellationToken),
                (messages, session, options, inner, cancellationToken) =>
                    RunStreamingAsync(messages, session, options, inner, cancellationToken))
            .Build();
    }

    private async Task<AgentResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent inner,
        CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        var outcome = "sucesso";
        string? exceptionType = null;
        try
        {
            var question = string.Concat(messages.Select(message => message.Text));
            var contextId = ConversationScope.ContextId;
            var entry = await _sessions.GetOrCreateAsync(inner, contextId, cancellationToken).ConfigureAwait(false);
            var state = await _workflow.RunAsync(question, entry.Conversation, cancellationToken).ConfigureAwait(false);
            _ = session;
            _ = options;
            return new AgentResponse(new ChatMessage(ChatRole.Assistant, state.Text));
        }
        catch (Exception exception)
        {
            outcome = "falha";
            exceptionType = exception.GetType().Name;
            return new AgentResponse(new ChatMessage(
                ChatRole.Assistant,
                "A interpretação não foi concluída. Nenhum número foi criado para esta resposta."));
        }
        finally
        {
            started.Stop();
            ExecutionLogMiddleware.WriteLine(session, started.ElapsedMilliseconds, outcome, exceptionType);
        }
    }

    private async IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent inner,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await RunAsync(messages, session, options, inner, cancellationToken).ConfigureAwait(false);
        yield return new AgentResponseUpdate(ChatRole.Assistant, response.Text);
    }
}
