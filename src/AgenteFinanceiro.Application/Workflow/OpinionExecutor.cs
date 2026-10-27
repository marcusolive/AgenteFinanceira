using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Workflow;

public sealed class OpinionExecutor
{
    private readonly AnswerComposer _composer;

    public OpinionExecutor(AnswerComposer composer)
    {
        _composer = composer;
    }

    public AnalysisState Execute(AnalysisState state, ConversationSession? session)
    {
        state.Text = _composer.Compose(state, session);
        if (state.Kind == AnalysisKind.Opinion && state.Text.Contains("Recomendação demonstrativa", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("O parecer não emite recomendação.");
        }

        return state;
    }
}
