using System.ComponentModel;
using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Tools;

public sealed class ComparisonTools
{
    private readonly AnswerComposer _composer;

    public ComparisonTools(AnswerComposer composer)
    {
        _composer = composer;
    }

    [Description("Compara categorias de despesa no mesmo mês. A diferença é a primeira menos a segunda.")]
    public string CompararCategorias(int year, int month, string primeira, string segunda, string? terceira)
    {
        var state = new AnalysisState
        {
            Kind = AnalysisKind.CompareCategories,
            Year = year,
            Month = month,
            ContextId = "ferramenta"
        };
        Add(state, primeira);
        Add(state, segunda);
        if (!string.IsNullOrWhiteSpace(terceira))
        {
            Add(state, terceira);
        }

        return _composer.Compose(state, null);
    }

    [Description("Compara a mesma categoria de despesa em dois meses.")]
    public string CompararCategoriaEntreMeses(string categoria, int yearA, int monthA, int yearB, int monthB)
    {
        var state = new AnalysisState
        {
            Kind = AnalysisKind.CompareAcrossMonths,
            Year = yearA,
            Month = monthA,
            EndYear = yearB,
            EndMonth = monthB,
            ContextId = "ferramenta"
        };
        Add(state, categoria);
        return _composer.Compose(state, null);
    }

    [Description("Informa que receita e despesa não entram no mesmo ranking.")]
    public string RecusarNaturezaMista()
    {
        var state = new AnalysisState { Kind = AnalysisKind.MixedNature, ContextId = "ferramenta" };
        return _composer.Compose(state, null);
    }

    private static void Add(AnalysisState state, string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        if (normalized.Contains("folha", StringComparison.Ordinal))
        {
            state.Categories.Add(ExpenseCategory.Folha);
        }
        else if (normalized.Contains("aluguel", StringComparison.Ordinal))
        {
            state.Categories.Add(ExpenseCategory.Aluguel);
        }
        else if (normalized.Contains("fornecedor", StringComparison.Ordinal))
        {
            state.Categories.Add(ExpenseCategory.Fornecedores);
        }
        else if (normalized.Contains("imposto", StringComparison.Ordinal))
        {
            state.Categories.Add(ExpenseCategory.Impostos);
        }
        else if (normalized.Contains("marketing", StringComparison.Ordinal))
        {
            state.Categories.Add(ExpenseCategory.Marketing);
        }
    }
}
