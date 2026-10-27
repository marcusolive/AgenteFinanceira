using System.Text;
using System.Text.Json;
using A2A;
using AgenteFinanceiro.Application.Middleware;

namespace AgenteFinanceiro.Tests.Integration;

public class A2AMessageSendTests : IClassFixture<AgenteFinanceiroApiFactory>
{
    private readonly HttpClient _client;

    public A2AMessageSendTests(AgenteFinanceiroApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Message_send_de_janeiro_devolve_a_base_e_o_log_sem_segredo()
    {
        ExecutionLog.Buffer.Clear();
        var payload = JsonSerializer.Serialize(
            new SendMessageRequest
            {
                Message = new Message
                {
                    MessageId = "m-janeiro",
                    Role = Role.User,
                    ContextId = "demo-aurora-001",
                    Parts = [Part.FromText("Qual o fluxo de caixa de janeiro de 2026?")]
                }
            },
            A2AJsonUtilities.DefaultOptions);
        var response = await _client.PostAsync(
            "/a2a/agente-financeiro/message:send",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("80000.00", body);
        Assert.Contains("20000.00", body);
        Assert.Contains("100000.00", body);
        Assert.Contains("87000.00", body);
        Assert.Contains("15000.00", body);
        Assert.Contains("13000.00", body);
        Assert.Contains("28000.00", body);
        Assert.Contains("message", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OpenAI:ApiKey", body);
        var log = string.Join('\n', ExecutionLog.Buffer.Lines);
        Assert.Contains("AgenteFinanceiro", log);
        Assert.DoesNotContain("Qual o fluxo de caixa", log);
        Assert.DoesNotContain("80000.00", log);
        Assert.DoesNotContain("OpenAI:ApiKey", log);
    }
}
