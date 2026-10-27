using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Domain.Finance;
using AgenteFinanceiro.Infrastructure.Data;

namespace AgenteFinanceiro.Tests.Calculations;

public class CashFlowCalculatorTests
{
    private readonly CashFlowCalculator _calculator = new(new InMemoryFinancialRepository());

    [Fact]
    public void Janeiro_fecha_a_cadeia_da_base()
    {
        Assert.True(_calculator.TryCalculate(new MonthPeriod(2026, 1), out var flow));
        Assert.NotNull(flow);
        Assert.Equal(100000.00m, flow.TotalRevenue);
        Assert.Equal(87000.00m, flow.TotalExpense);
        Assert.Equal(15000.00m, flow.OpeningBalance);
        Assert.Equal(13000.00m, flow.Result);
        Assert.Equal(28000.00m, flow.ClosingBalance);
        Assert.False(flow.IsNegative);
    }

    [Fact]
    public void Fevereiro_e_o_unico_resultado_negativo_da_cadeia()
    {
        Assert.True(_calculator.TryCalculate(new MonthPeriod(2026, 2), out var flow));
        Assert.NotNull(flow);
        Assert.Equal(55000.00m, flow.TotalRevenue);
        Assert.Equal(79000.00m, flow.TotalExpense);
        Assert.Equal(28000.00m, flow.OpeningBalance);
        Assert.Equal(-24000.00m, flow.Result);
        Assert.Equal(4000.00m, flow.ClosingBalance);
        Assert.True(flow.IsNegative);
    }

    [Fact]
    public void Marco_reabre_com_o_saldo_final_de_fevereiro()
    {
        Assert.True(_calculator.TryCalculate(new MonthPeriod(2026, 3), out var flow));
        Assert.NotNull(flow);
        Assert.Equal(85000.00m, flow.TotalRevenue);
        Assert.Equal(77000.00m, flow.TotalExpense);
        Assert.Equal(4000.00m, flow.OpeningBalance);
        Assert.Equal(8000.00m, flow.Result);
        Assert.Equal(12000.00m, flow.ClosingBalance);
        Assert.False(flow.IsNegative);
    }

    [Fact]
    public void Mes_fora_de_1_a_12_e_rejeitado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonthPeriod(2026, 13));
    }
}
