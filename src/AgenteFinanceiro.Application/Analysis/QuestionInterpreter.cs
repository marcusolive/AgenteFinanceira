using System.Globalization;
using System.Text.RegularExpressions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Application.Analysis;

public enum AnalysisKind
{
    CashFlow,
    NegativeScan,
    CompareCategories,
    CompareAcrossMonths,
    Opinion,
    Reduction,
    Approval,
    Rejection,
    Ambiguous,
    GapPeriod,
    GapCategory,
    GapAliquot,
    GapDocument,
    GapMutation,
    GapTransfer,
    MissingPeriod,
    VagueReduction,
    Empty,
    MixedNature
}

public sealed class AnalysisState
{
    public AnalysisKind Kind { get; set; }

    public string ContextId { get; set; } = string.Empty;

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int? EndYear { get; set; }

    public int? EndMonth { get; set; }

    public List<ExpenseCategory> Categories { get; } = [];

    public decimal? Percent { get; set; }

    public string? UnknownLabel { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool AsksForApproval { get; set; }

    public string? CheckpointId { get; set; }

    public ApprovalState Decision { get; set; } = ApprovalState.NotRequired;

    public string StateName { get; set; } = ApprovalState.NotRequired.ToString();

    public decimal ExpenseBase { get; set; }

    public string? CategoryName { get; set; }

    public bool RememberPeriod { get; set; } = true;
}

public static partial class QuestionInterpreter
{
    public static AnalysisState Interpret(string? question, string contextId)
    {
        var state = new AnalysisState { ContextId = contextId };
        var text = (question ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            state.Kind = AnalysisKind.Empty;
            return state;
        }

        var normalized = text.ToLowerInvariant()
            .Replace("ç", "c", StringComparison.Ordinal)
            .Replace("á", "a", StringComparison.Ordinal)
            .Replace("à", "a", StringComparison.Ordinal)
            .Replace("ã", "a", StringComparison.Ordinal)
            .Replace("é", "e", StringComparison.Ordinal)
            .Replace("ê", "e", StringComparison.Ordinal)
            .Replace("í", "i", StringComparison.Ordinal)
            .Replace("ó", "o", StringComparison.Ordinal)
            .Replace("ô", "o", StringComparison.Ordinal)
            .Replace("õ", "o", StringComparison.Ordinal)
            .Replace("ú", "u", StringComparison.Ordinal);

        if (ContainsAny(normalized, "transf", "pagamento", "pagar ", "pague"))
        {
            state.Kind = AnalysisKind.GapTransfer;
            return state;
        }

        if (ContainsAny(normalized, "documento", "nota fiscal", "nfe", "nf-e"))
        {
            state.Kind = AnalysisKind.GapDocument;
            return state;
        }

        if (normalized.Contains("aliquota", StringComparison.Ordinal))
        {
            state.Kind = AnalysisKind.GapAliquot;
            return state;
        }

        if (ContainsAny(normalized, "inclua", "incluir", "adicione", "altere", "alterar", "exclua", "apague"))
        {
            state.Kind = AnalysisKind.GapMutation;
            return state;
        }

        if (IsDecision(normalized, "talvez"))
        {
            state.Kind = AnalysisKind.Ambiguous;
            state.RememberPeriod = false;
            return state;
        }

        if (IsDecision(normalized, "aprovo") || normalized is "aprovado" || normalized.StartsWith("sim, aprovo", StringComparison.Ordinal))
        {
            state.Kind = AnalysisKind.Approval;
            state.RememberPeriod = false;
            return state;
        }

        if (ContainsAny(normalized, "recuso", "rejeito", "nao aprovo"))
        {
            state.Kind = AnalysisKind.Rejection;
            state.RememberPeriod = false;
            return state;
        }

        ReadPeriod(normalized, state);
        ReadCategories(normalized, state);

        if (normalized.Contains("viagens", StringComparison.Ordinal))
        {
            state.Kind = AnalysisKind.GapCategory;
            state.UnknownLabel = "Viagens";
            return state;
        }

        if (state.Month is > 0 && state.Year is > 0 && !IsKnownBaseMonth(state.Year.Value, state.Month.Value) && !normalized.Contains("compar", StringComparison.Ordinal) && !normalized.Contains("negativ", StringComparison.Ordinal))
        {
            state.Kind = AnalysisKind.GapPeriod;
            return state;
        }

        if (ContainsAny(normalized, "reduz", "recomend"))
        {
            state.Percent = ReadPercent(normalized);
            state.Kind = state.Percent is null ? AnalysisKind.VagueReduction : AnalysisKind.Reduction;
            return state;
        }

        if (normalized.Contains("parecer", StringComparison.Ordinal))
        {
            state.Kind = AnalysisKind.Opinion;
            return state;
        }

        if (normalized.Contains("negativ", StringComparison.Ordinal))
        {
            state.Kind = AnalysisKind.NegativeScan;
            if (state.EndYear is null && normalized.Contains("novembro", StringComparison.Ordinal))
            {
                state.Year = 2025;
                state.Month = 11;
                state.EndYear = 2026;
                state.EndMonth = 2;
            }

            return state;
        }

        if (normalized.Contains("compar", StringComparison.Ordinal) || normalized.Contains(" contra ", StringComparison.Ordinal))
        {
            if (normalized.Contains("vendas", StringComparison.Ordinal))
            {
                state.Kind = AnalysisKind.MixedNature;
                return state;
            }

            state.Kind = state.Categories.Count == 1 && HasTwoMonths(normalized)
                ? AnalysisKind.CompareAcrossMonths
                : AnalysisKind.CompareCategories;
            return state;
        }

        if (state.Month is null)
        {
            state.Kind = AnalysisKind.MissingPeriod;
            return state;
        }

        state.Kind = AnalysisKind.CashFlow;
        return state;
    }

    private static bool HasTwoMonths(string normalized) =>
        (normalized.Contains("janeiro", StringComparison.Ordinal) && normalized.Contains("fevereiro", StringComparison.Ordinal))
        || (normalized.Contains("fevereiro", StringComparison.Ordinal) && normalized.Contains("marco", StringComparison.Ordinal));

    private static bool IsKnownBaseMonth(int year, int month) =>
        year == 2026 && month is >= 1 and <= 3;

    private static bool IsDecision(string normalized, string word) =>
        normalized == word || normalized.StartsWith(word + " ", StringComparison.Ordinal) || normalized.EndsWith(" " + word, StringComparison.Ordinal);

    private static bool ContainsAny(string text, params string[] words) =>
        words.Any(word => text.Contains(word, StringComparison.Ordinal));

    private static void ReadCategories(string normalized, AnalysisState state)
    {
        Add(normalized, "folha", ExpenseCategory.Folha, state);
        Add(normalized, "aluguel", ExpenseCategory.Aluguel, state);
        Add(normalized, "fornecedor", ExpenseCategory.Fornecedores, state);
        Add(normalized, "imposto", ExpenseCategory.Impostos, state);
        Add(normalized, "marketing", ExpenseCategory.Marketing, state);
    }

    private static void Add(string text, string token, ExpenseCategory category, AnalysisState state)
    {
        if (text.Contains(token, StringComparison.Ordinal))
        {
            state.Categories.Add(category);
        }
    }

    private static void ReadPeriod(string normalized, AnalysisState state)
    {
        (string Name, int Month)[] names =
        [
            ("janeiro", 1), ("fevereiro", 2), ("marco", 3), ("abril", 4), ("maio", 5),
            ("junho", 6), ("julho", 7), ("agosto", 8), ("setembro", 9), ("outubro", 10),
            ("novembro", 11), ("dezembro", 12)
        ];

        var found = names
            .Select(item => (item.Month, Index: normalized.IndexOf(item.Name, StringComparison.Ordinal)))
            .Where(item => item.Index >= 0)
            .OrderBy(item => item.Index)
            .ToArray();

        if (found.Length > 0)
        {
            state.Month = found[0].Month;
        }

        if (found.Length > 1)
        {
            state.EndMonth = found[^1].Month;
        }

        var key = PeriodKeyRegex().Match(normalized);
        if (key.Success)
        {
            state.Year = int.Parse(key.Groups[1].Value, CultureInfo.InvariantCulture);
            state.Month = int.Parse(key.Groups[2].Value, CultureInfo.InvariantCulture);
        }

        var years = YearRegex().Matches(normalized);
        if (years.Count > 0 && state.Year is null)
        {
            state.Year = int.Parse(years[0].Value, CultureInfo.InvariantCulture);
        }

        if (years.Count > 1)
        {
            state.EndYear = int.Parse(years[^1].Value, CultureInfo.InvariantCulture);
        }

        if (state.Month is not null && state.Year is null)
        {
            state.Year = 2026;
        }

        if (state.EndMonth is not null && state.EndYear is null)
        {
            state.EndYear = state.Year;
        }
    }

    private static decimal? ReadPercent(string normalized)
    {
        if (normalized.Contains("um quarto", StringComparison.Ordinal))
        {
            return 25m;
        }

        if (normalized.Contains("um quinto", StringComparison.Ordinal))
        {
            return 20m;
        }

        var withoutThreshold = normalized
            .Replace("mais de 20%", "", StringComparison.Ordinal)
            .Replace("superior a 20%", "", StringComparison.Ordinal)
            .Replace("mais de 20 %", "", StringComparison.Ordinal);
        var match = PercentRegex().Match(withoutThreshold);
        if (!match.Success)
        {
            return null;
        }

        var raw = match.Groups[1].Value.Replace(',', '.');
        return decimal.Parse(raw, CultureInfo.InvariantCulture);
    }

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*%")]
    private static partial Regex PercentRegex();

    [GeneratedRegex(@"(20\d{2})-(\d{2})")]
    private static partial Regex PeriodKeyRegex();

    [GeneratedRegex(@"20\d{2}")]
    private static partial Regex YearRegex();
}
