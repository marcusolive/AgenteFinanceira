namespace AgenteFinanceiro.Domain.Finance;

public sealed class ReductionAssessment
{
    public required MonthPeriod Period { get; init; }

    public ExpenseCategory? Category { get; init; }

    public decimal? Percent { get; init; }

    public decimal ExpenseBase { get; init; }

    public decimal? ReductionAmount { get; init; }

    public ApprovalState Decision { get; init; }
}
