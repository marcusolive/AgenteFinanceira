namespace AgenteFinanceiro.Domain.Finance;

public sealed record MonthPeriod
{
    public int Year { get; }

    public int Month { get; }

    public MonthPeriod(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), "O mês deve estar entre 1 e 12.");
        }

        Year = year;
        Month = month;
    }

    public string Key => $"{Year:0000}-{Month:00}";

    public override string ToString() => Key;

    public static bool TryAdvance(MonthPeriod current, out MonthPeriod next)
    {
        if (current.Month == 12)
        {
            next = new MonthPeriod(current.Year + 1, 1);
            return true;
        }

        next = new MonthPeriod(current.Year, current.Month + 1);
        return true;
    }
}
