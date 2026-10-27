namespace AgenteFinanceiro.Domain.Finance;

public sealed class MonthlyCashFlow
{
    public MonthlyCashFlow(
        MonthPeriod period,
        IReadOnlyList<RevenueLine> revenues,
        IReadOnlyList<ExpenseLine> expenses,
        decimal openingBalance)
    {
        Period = period;
        Revenues = revenues;
        Expenses = expenses;
        OpeningBalance = openingBalance;
        TotalRevenue = Sum(revenues, line => line.Amount);
        TotalExpense = Sum(expenses, line => line.Amount);
    }

    public MonthPeriod Period { get; }

    public IReadOnlyList<RevenueLine> Revenues { get; }

    public IReadOnlyList<ExpenseLine> Expenses { get; }

    public decimal TotalRevenue { get; }

    public decimal TotalExpense { get; }

    public decimal OpeningBalance { get; }

    public decimal Result => TotalRevenue - TotalExpense;

    public decimal ClosingBalance => OpeningBalance + Result;

    public bool IsNegative => Result < 0m;

    private static decimal Sum<T>(IReadOnlyList<T> items, Func<T, decimal> amount)
    {
        decimal total = 0m;
        foreach (var item in items)
        {
            total += amount(item);
        }

        return total;
    }
}
