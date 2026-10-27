using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Workflow;

public sealed class CalculationExecutor
{
    private readonly ICashFlowCalculator _calculator;

    public CalculationExecutor(ICashFlowCalculator calculator)
    {
        _calculator = calculator;
    }

    public AnalysisState Execute(AnalysisState state)
    {
        if (state.Kind == AnalysisKind.CashFlow && state.Year is not null && state.Month is not null)
        {
            _calculator.TryCalculate(new MonthPeriod(state.Year.Value, state.Month.Value), out _);
        }

        if (state.Kind == AnalysisKind.Reduction && state.Year is not null && state.Month is not null && state.Percent is not null)
        {
            var assessment = _calculator.PrepareReduction(new MonthPeriod(state.Year.Value, state.Month.Value), null, state.Percent);
            state.ExpenseBase = assessment.ExpenseBase;
            state.Decision = assessment.Decision;
            state.StateName = assessment.Decision.ToString();
            state.AsksForApproval = assessment.Decision == ApprovalState.Pending;
        }

        return state;
    }
}
