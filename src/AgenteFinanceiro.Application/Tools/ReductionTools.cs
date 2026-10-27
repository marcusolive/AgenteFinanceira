using System.ComponentModel;
using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Tools;

public sealed class ReductionTools
{
    private readonly ICashFlowCalculator _calculator;

    public ReductionTools(ICashFlowCalculator calculator)
    {
        _calculator = calculator;
    }

    [Description("Prepara redução de despesas. Acima de 20% devolve aprovação pendente, sem o valor do corte.")]
    public ReductionAssessment PrepararRecomendacaoReducao(int year, int month, decimal percent)
    {
        var before = _calculator.TryCalculate(new MonthPeriod(year, month), out var flow) ? flow?.ClosingBalance : null;
        var assessment = _calculator.PrepareReduction(new MonthPeriod(year, month), null, percent);
        _calculator.TryCalculate(new MonthPeriod(year, month), out var after);
        if (before != after?.ClosingBalance)
        {
            throw new InvalidOperationException("A ferramenta alterou o fluxo de caixa.");
        }

        return assessment;
    }
}
