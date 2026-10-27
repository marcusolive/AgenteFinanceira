using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Infrastructure.Data;

public static class AuroraFinancialSeed
{
    public static IReadOnlyList<MonthSnapshotData> Months { get; } =
    [
        Month(2026, 1, 15000.00m,
            (RevenueSource.Vendas, 80000.00m),
            (RevenueSource.Servicos, 20000.00m),
            (ExpenseCategory.Folha, 40000.00m),
            (ExpenseCategory.Aluguel, 8000.00m),
            (ExpenseCategory.Fornecedores, 22000.00m),
            (ExpenseCategory.Impostos, 12000.00m),
            (ExpenseCategory.Marketing, 5000.00m)),
        Month(2026, 2, 28000.00m,
            (RevenueSource.Vendas, 45000.00m),
            (RevenueSource.Servicos, 10000.00m),
            (ExpenseCategory.Folha, 40000.00m),
            (ExpenseCategory.Aluguel, 8000.00m),
            (ExpenseCategory.Fornecedores, 18000.00m),
            (ExpenseCategory.Impostos, 7000.00m),
            (ExpenseCategory.Marketing, 6000.00m)),
        Month(2026, 3, 4000.00m,
            (RevenueSource.Vendas, 70000.00m),
            (RevenueSource.Servicos, 15000.00m),
            (ExpenseCategory.Folha, 42000.00m),
            (ExpenseCategory.Aluguel, 8000.00m),
            (ExpenseCategory.Fornecedores, 15000.00m),
            (ExpenseCategory.Impostos, 9000.00m),
            (ExpenseCategory.Marketing, 3000.00m))
    ];

    private static MonthSnapshotData Month(
        int year,
        int month,
        decimal opening,
        (RevenueSource Source, decimal Amount) revenueA,
        (RevenueSource Source, decimal Amount) revenueB,
        params (ExpenseCategory Category, decimal Amount)[] expenses)
    {
        var period = new MonthPeriod(year, month);
        return new MonthSnapshotData(
            period,
            opening,
            [
                new RevenueLine(period, revenueA.Source, revenueA.Amount),
                new RevenueLine(period, revenueB.Source, revenueB.Amount)
            ],
            expenses.Select(item => new ExpenseLine(period, item.Category, item.Amount)).ToArray());
    }
}

public sealed record MonthSnapshotData(
    MonthPeriod Period,
    decimal OpeningBalance,
    IReadOnlyList<RevenueLine> Revenues,
    IReadOnlyList<ExpenseLine> Expenses);
