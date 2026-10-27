using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Infrastructure.Data;

public sealed class InMemoryFinancialRepository : IFinancialDataRepository
{
    private readonly IReadOnlyDictionary<string, MonthSnapshot> _months;

    public InMemoryFinancialRepository()
    {
        _months = AuroraFinancialSeed.Months.ToDictionary(
            month => month.Period.Key,
            month => new MonthSnapshot
            {
                Period = month.Period,
                OpeningBalance = month.OpeningBalance,
                Revenues = month.Revenues.ToArray(),
                Expenses = month.Expenses.ToArray()
            });
    }

    public IReadOnlyList<ExpenseCategory> ExpenseCategories { get; } =
    [
        ExpenseCategory.Folha,
        ExpenseCategory.Aluguel,
        ExpenseCategory.Fornecedores,
        ExpenseCategory.Impostos,
        ExpenseCategory.Marketing
    ];

    public IReadOnlyList<MonthPeriod> ListPeriods() =>
        _months.Values.Select(month => month.Period).OrderBy(period => period.Key).ToArray();

    public bool TryGetSnapshot(MonthPeriod period, out MonthSnapshot? snapshot)
    {
        if (_months.TryGetValue(period.Key, out var found))
        {
            snapshot = found;
            return true;
        }

        snapshot = null;
        return false;
    }
}
