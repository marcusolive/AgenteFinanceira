using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Domain.Abstractions;

public interface ICashFlowCalculator
{
    bool TryCalculate(MonthPeriod period, out MonthlyCashFlow? flow);

    IReadOnlyList<MonthlyCashFlow> ListNegative(
        MonthPeriod start,
        MonthPeriod end,
        out IReadOnlyList<MonthPeriod> missing);

    CategoryComparison CompareCategories(MonthPeriod period, IReadOnlyList<ExpenseCategory> categories);

    bool TryCompareCategoryAcrossMonths(
        ExpenseCategory category,
        IReadOnlyList<MonthPeriod> periods,
        out CrossMonthComparison? comparison,
        out IReadOnlyList<MonthPeriod> missing);

    ReductionAssessment PrepareReduction(MonthPeriod period, ExpenseCategory? category, decimal? percent);

    decimal QuoteReduction(decimal expenseBase, decimal percent);
}
