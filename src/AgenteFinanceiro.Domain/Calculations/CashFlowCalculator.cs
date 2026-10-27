using AgenteFinanceiro.Domain.Abstractions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Domain.Calculations;

public sealed class CashFlowCalculator : ICashFlowCalculator
{
    private readonly IFinancialDataRepository _repository;

    public CashFlowCalculator(IFinancialDataRepository repository)
    {
        _repository = repository;
    }

    public bool TryCalculate(MonthPeriod period, out MonthlyCashFlow? flow)
    {
        if (!_repository.TryGetSnapshot(period, out var snapshot) || snapshot is null)
        {
            flow = null;
            return false;
        }

        flow = new MonthlyCashFlow(snapshot.Period, snapshot.Revenues, snapshot.Expenses, snapshot.OpeningBalance);
        return true;
    }

    public IReadOnlyList<MonthlyCashFlow> ListNegative(
        MonthPeriod start,
        MonthPeriod end,
        out IReadOnlyList<MonthPeriod> missing)
    {
        var absent = new List<MonthPeriod>();
        var negative = new List<MonthlyCashFlow>();
        foreach (var period in Enumerate(start, end))
        {
            if (!TryCalculate(period, out var flow) || flow is null)
            {
                absent.Add(period);
                continue;
            }

            if (flow.IsNegative)
            {
                negative.Add(flow);
            }
        }

        missing = absent;
        return negative;
    }

    public CategoryComparison CompareCategories(MonthPeriod period, IReadOnlyList<ExpenseCategory> categories)
    {
        if (!TryCalculate(period, out var flow) || flow is null)
        {
            return new CategoryComparison { Period = period };
        }

        var items = categories
            .Select(category => new CategoryAmount(
                category,
                flow.Expenses.Single(line => line.Category == category).Amount))
            .ToArray();

        decimal? difference = items.Length == 2 ? items[0].Amount - items[1].Amount : null;
        var largest = items.Length == 0
            ? (ExpenseCategory?)null
            : items.OrderByDescending(item => item.Amount).First().Category;

        return new CategoryComparison
        {
            Period = period,
            Items = items,
            Difference = difference,
            Largest = largest
        };
    }

    public bool TryCompareCategoryAcrossMonths(
        ExpenseCategory category,
        IReadOnlyList<MonthPeriod> periods,
        out CrossMonthComparison? comparison,
        out IReadOnlyList<MonthPeriod> missing)
    {
        var absent = new List<MonthPeriod>();
        var points = new List<MonthAmount>();
        foreach (var period in periods)
        {
            if (!TryCalculate(period, out var flow) || flow is null)
            {
                absent.Add(period);
                continue;
            }

            var amount = flow.Expenses.Single(line => line.Category == category).Amount;
            points.Add(new MonthAmount(period, amount));
        }

        missing = absent;
        comparison = new CrossMonthComparison
        {
            Category = category,
            Points = points,
            Difference = points.Count == 2 ? points[0].Amount - points[1].Amount : null
        };
        return absent.Count == 0;
    }

    public ReductionAssessment PrepareReduction(MonthPeriod period, ExpenseCategory? category, decimal? percent)
    {
        if (!TryCalculate(period, out var flow) || flow is null)
        {
            return new ReductionAssessment
            {
                Period = period,
                Category = category,
                Percent = percent,
                Decision = ApprovalState.Pending
            };
        }

        decimal expenseBase = category is null
            ? flow.TotalExpense
            : flow.Expenses.Single(line => line.Category == category).Amount;

        if (percent is null)
        {
            return new ReductionAssessment
            {
                Period = period,
                Category = category,
                ExpenseBase = expenseBase,
                Decision = ApprovalState.Pending
            };
        }

        bool requiresApproval = percent.Value > 20m;
        return new ReductionAssessment
        {
            Period = period,
            Category = category,
            Percent = percent,
            ExpenseBase = expenseBase,
            ReductionAmount = requiresApproval ? null : QuoteReduction(expenseBase, percent.Value),
            Decision = requiresApproval ? ApprovalState.Pending : ApprovalState.NotRequired
        };
    }

    public decimal QuoteReduction(decimal expenseBase, decimal percent) =>
        decimal.Round(expenseBase * percent / 100m, 2, MidpointRounding.AwayFromZero);

    private static IEnumerable<MonthPeriod> Enumerate(MonthPeriod start, MonthPeriod end)
    {
        var current = start;
        while (current.Year < end.Year || (current.Year == end.Year && current.Month <= end.Month))
        {
            yield return current;
            MonthPeriod.TryAdvance(current, out current);
        }
    }
}
