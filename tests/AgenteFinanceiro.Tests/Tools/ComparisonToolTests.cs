using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Application.Tools;
using AgenteFinanceiro.Domain.Calculations;
using AgenteFinanceiro.Infrastructure.Data;

namespace AgenteFinanceiro.Tests.Tools;

public class ComparisonToolTests
{
    private readonly ComparisonTools _tools = new(
        new AnswerComposer(new InMemoryFinancialRepository(), new CashFlowCalculator(new InMemoryFinancialRepository())));

    [Fact]
    public void Folha_menos_marketing_em_janeiro()
    {
        var text = _tools.CompararCategorias(2026, 1, "Folha", "Marketing", null);
        Assert.Contains("Folha 40000.00", text);
        Assert.Contains("Marketing 5000.00", text);
        Assert.Contains("35000.00", text);
        Assert.Contains("Maior: Folha", text);
    }

    [Fact]
    public void Tres_categorias_apontam_folha_como_maior()
    {
        var text = _tools.CompararCategorias(2026, 1, "Folha", "Aluguel", "Marketing");
        Assert.Contains("40000.00", text);
        Assert.Contains("8000.00", text);
        Assert.Contains("5000.00", text);
        Assert.Contains("Maior: Folha", text);
    }

    [Fact]
    public void Folha_entre_janeiro_e_fevereiro_tem_diferenca_zero()
    {
        var text = _tools.CompararCategoriaEntreMeses("Folha", 2026, 1, 2026, 2);
        Assert.Contains("40000.00", text);
        Assert.Contains("Diferença 0.00", text);
    }

    [Fact]
    public void Vendas_contra_folha_nao_mistura_ranking()
    {
        var text = _tools.RecusarNaturezaMista();
        Assert.Contains("não produz ranking de despesa misto", text);
        Assert.DoesNotContain("Maior:", text);
    }
}
