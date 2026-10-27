using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Workflow;

public sealed class ValidationExecutor
{
    public AnalysisState Execute(AnalysisState state, ConversationSession? session)
    {
        if (state.Month is null && session?.EstablishedPeriod is not null &&
            state.Kind is AnalysisKind.CashFlow or AnalysisKind.CompareCategories or AnalysisKind.Opinion or AnalysisKind.Reduction)
        {
            state.Year = session.EstablishedPeriod.Year;
            state.Month = session.EstablishedPeriod.Month;
        }

        if (state.Kind == AnalysisKind.GapPeriod || (state.Month is > 12))
        {
            state.Decision = ApprovalState.NotRequired;
        }

        return state;
    }
}
