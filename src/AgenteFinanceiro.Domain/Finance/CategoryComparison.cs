namespace AgenteFinanceiro.Domain.Finance;

public readonly record struct CategoryAmount(ExpenseCategory Category, decimal Amount);

public sealed class CategoryComparison
{
    public MonthPeriod? Period { get; init; }

    public IReadOnlyList<CategoryAmount> Items { get; init; } = [];

    public decimal? Difference { get; init; }

    public ExpenseCategory? Largest { get; init; }

    public bool MixedNature { get; init; }
}

public sealed class CrossMonthComparison
{
    public ExpenseCategory Category { get; init; }

    public IReadOnlyList<MonthAmount> Points { get; init; } = [];

    public decimal? Difference { get; init; }
}

public readonly record struct MonthAmount(MonthPeriod Period, decimal Amount);
