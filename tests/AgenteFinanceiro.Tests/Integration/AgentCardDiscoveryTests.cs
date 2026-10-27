namespace AgenteFinanceiro.Tests.Integration;

public class AgentCardDiscoveryTests : IClassFixture<AgenteFinanceiroApiFactory>
{
    private readonly HttpClient _client;

    public AgentCardDiscoveryTests(AgenteFinanceiroApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Cartao_publica_nome_url_binding_e_seis_skills()
    {
        var response = await _client.GetAsync("/.well-known/agent-card.json");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("AgenteFinanceiro", body);
        Assert.Contains("http://localhost:5100/a2a/agente-financeiro", body);
        Assert.Contains("HTTP+JSON", body);
        Assert.Contains("consultar-periodo", body);
        Assert.Contains("calcular-fluxo-caixa-mensal", body);
        Assert.Contains("identificar-resultado-negativo", body);
        Assert.Contains("comparar-categorias", body);
        Assert.Contains("gerar-parecer", body);
        Assert.Contains("recomendar-reducao", body);
        Assert.DoesNotContain("SOFIA", body);
        Assert.DoesNotContain("OpenAI:ApiKey", body);
    }

    [Fact]
    public async Task Health_responde_200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
