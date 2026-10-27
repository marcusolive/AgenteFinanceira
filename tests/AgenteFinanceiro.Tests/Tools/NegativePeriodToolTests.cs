using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Infrastructure.Data;

namespace AgenteFinanceiro.Tests.Tools;

public class NegativePeriodToolTests
{
    private readonly CashFlowTools _tools;

    public NegativePeriodToolTests()
    {
        var repository = new InMemoryFinancialRepository();
        var calculator = new CashFlowCalculator(repository);
        _tools = new CashFlowTools(new AnswerComposer(repository, calculator), calculator);
    }

    [Fact]
    public void A_base_inteira_cita_somente_fevereiro()
    {
        var text = _tools.ListarPeriodosNegativos(2026, 1, 2026, 3);
        Assert.Contains("2026-02", text);
        Assert.Contains("-24000.00", text);
        Assert.Contains("28000.00", text);
        Assert.Contains("4000.00", text);
        Assert.DoesNotContain("2026-01", text);
        Assert.DoesNotContain("2026-03", text);
    }

    [Fact]
    public void Janeiro_nao_e_negativo_e_mostra_13000()
    {
        var text = _tools.ListarPeriodosNegativos(2026, 1, 2026, 1);
        Assert.Contains("não é negativo", text);
        Assert.Contains("13000.00", text);
        Assert.DoesNotContain("-24000.00", text);
    }

    [Fact]
    public void Intervalo_com_meses_ausentes_nao_preenche_zero()
    {
        var text = _tools.ListarPeriodosNegativos(2025, 11, 2026, 2);
        Assert.Contains("2025-11", text);
        Assert.Contains("2025-12", text);
        Assert.Contains("2026-02", text);
        Assert.Contains("-24000.00", text);
        Assert.Contains("sem valor inventado", text);
        Assert.DoesNotContain("2025-11 0", text);
        Assert.DoesNotContain("2025-12 0", text);
    }
}
