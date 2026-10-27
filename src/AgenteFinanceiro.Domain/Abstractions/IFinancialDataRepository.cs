using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Domain.Abstractions;

public sealed class MonthSnapshot
{
    public required MonthPeriod Period { get; init; }

    public required decimal OpeningBalance { get; init; }

    public required IReadOnlyList<RevenueLine> Revenues { get; init; }

    public required IReadOnlyList<ExpenseLine> Expenses { get; init; }
}

public interface IFinancialDataRepository
{
    IReadOnlyList<MonthPeriod> ListPeriods();

    bool TryGetSnapshot(MonthPeriod period, out MonthSnapshot? snapshot);

    IReadOnlyList<ExpenseCategory> ExpenseCategories { get; }
}
