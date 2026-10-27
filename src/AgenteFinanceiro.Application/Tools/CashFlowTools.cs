using System.ComponentModel;
using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Tools;

public sealed class CashFlowTools
{
    private readonly AnswerComposer _composer;
    private readonly ICashFlowCalculator _calculator;

    public CashFlowTools(AnswerComposer composer, ICashFlowCalculator calculator)
    {
        _composer = composer;
        _calculator = calculator;
    }

    [Description("Obtém receitas, despesas, saldos e resultado de um mês da base Aurora.")]
    public string ObterFluxoCaixaMes(int year, int month)
    {
        var state = new AnalysisState
        {
            Kind = AnalysisKind.CashFlow,
            Year = year,
            Month = month,
            ContextId = "ferramenta"
        };
        return _composer.Compose(state, null);
    }

    [Description("Lista períodos com resultado menor que zero. Meses ausentes são nomeados e não viram zero.")]
    public string ListarPeriodosNegativos(int startYear, int startMonth, int endYear, int endMonth)
    {
        var state = new AnalysisState
        {
            Kind = AnalysisKind.NegativeScan,
            Year = startYear,
            Month = startMonth,
            EndYear = endYear,
            EndMonth = endMonth,
            ContextId = "ferramenta"
        };
        if (startYear == 2026 && startMonth == 1 && endYear == 2026 && endMonth == 1)
        {
            return _composer.Compose(state, null);
        }

        state.EndMonth = endMonth;
        state.EndYear = endYear;
        return _composer.Compose(state, null);
    }

    public ICashFlowCalculator Calculator => _calculator;
}
