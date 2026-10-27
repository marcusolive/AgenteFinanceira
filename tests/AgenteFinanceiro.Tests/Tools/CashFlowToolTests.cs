using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Infrastructure.Data;

namespace AgenteFinanceiro.Tests.Tools;

public class CashFlowToolTests
{
    private readonly CashFlowTools _tools = new(
        new AnswerComposer(new InMemoryFinancialRepository(), new CashFlowCalculator(new InMemoryFinancialRepository())),
        new CashFlowCalculator(new InMemoryFinancialRepository()));

    [Theory]
    [InlineData(1, "100000.00", "87000.00", "15000.00", "13000.00", "28000.00")]
    [InlineData(2, "55000.00", "79000.00", "28000.00", "-24000.00", "4000.00")]
    [InlineData(3, "85000.00", "77000.00", "4000.00", "8000.00", "12000.00")]
    public void ObterFluxoCaixaMes_devolve_os_tres_meses(int month, string revenue, string expense, string opening, string result, string closing)
    {
        var text = _tools.ObterFluxoCaixaMes(2026, month);
        Assert.Contains(revenue, text);
        Assert.Contains(expense, text);
        Assert.Contains(opening, text);
        Assert.Contains(result, text);
        Assert.Contains(closing, text);
        Assert.Contains("sem pedido de aprovação", text);
    }

    [Fact]
    public void Janeiro_repete_os_mesmos_centavos_e_nao_pede_aprovacao()
    {
        var first = _tools.ObterFluxoCaixaMes(2026, 1);
        var second = _tools.ObterFluxoCaixaMes(2026, 1);
        Assert.Equal(first, second);
        Assert.Contains("Vendas 80000.00", first);
        Assert.Contains("Serviços 20000.00", first);
        Assert.Contains("Folha 40000.00", first);
        Assert.Contains("Aluguel 8000.00", first);
        Assert.Contains("Fornecedores 22000.00", first);
        Assert.Contains("Impostos 12000.00", first);
        Assert.Contains("Marketing 5000.00", first);
        Assert.DoesNotContain("aguarda aprovação", first);
    }
}
