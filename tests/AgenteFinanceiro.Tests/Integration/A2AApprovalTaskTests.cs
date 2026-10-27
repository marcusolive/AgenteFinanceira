using System.Net;
using System.Text;
using System.Text.Json;
using A2A;

namespace AgenteFinanceiro.Tests.Integration;

public class A2AApprovalTaskTests : IClassFixture<AgenteFinanceiroApiFactory>
{
    private readonly HttpClient _client;
    private readonly AgenteFinanceiroApiFactory _factory;

    public A2AApprovalTaskTests(AgenteFinanceiroApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reducao_de_25_porcento_pede_task_e_aprovacao_entrega_artifact()
    {
        var contextId = "aprovacao-" + Guid.NewGuid().ToString("N");
        var pendingBody = await SendAsync(contextId, "Recomende reduzir 25% das despesas de janeiro de 2026.");
        var pending = JsonSerializer.Deserialize<SendMessageResponse>(pendingBody, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(pending?.Task);
        Assert.Equal(TaskState.InputRequired, pending.Task.Status.State);
        Assert.Contains("TASK_STATE_INPUT_REQUIRED", pendingBody);
        Assert.DoesNotContain("21750.00", pendingBody);
        Assert.DoesNotContain("Recomendação demonstrativa", pendingBody);
        Assert.True(pending.Task.Artifacts is null || pending.Task.Artifacts.Count == 0);
        var checkpointJson = ReadCheckpoints();
        Assert.Contains(pending.Task.Id, checkpointJson);

        var approvedBody = await SendAsync(contextId, "aprovo");
        var approved = JsonSerializer.Deserialize<SendMessageResponse>(approvedBody, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(approved?.Task);
        Assert.Equal(pending.Task.Id, approved.Task.Id);
        Assert.Equal(TaskState.Completed, approved.Task.Status.State);
        Assert.Contains("TASK_STATE_COMPLETED", approvedBody);
        var artifactText = string.Concat(approved.Task.Artifacts?.SelectMany(artifact => artifact.Parts ?? []).Select(part => part.Text) ?? []);
        Assert.Contains("Recomendação demonstrativa", artifactText);
        Assert.Contains("21750.00", artifactText);
    }

    [Fact]
    public async Task Recusa_conclui_a_task_sem_artifact_de_recomendacao()
    {
        var contextId = "recusa-" + Guid.NewGuid().ToString("N");
        var pendingBody = await SendAsync(contextId, "Recomende reduzir 25% das despesas de janeiro de 2026.");
        var pending = JsonSerializer.Deserialize<SendMessageResponse>(pendingBody, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(pending?.Task);

        var refusedBody = await SendAsync(contextId, "recuso");
        var refused = JsonSerializer.Deserialize<SendMessageResponse>(refusedBody, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(refused?.Task);
        Assert.Equal(pending.Task.Id, refused.Task.Id);
        Assert.Equal(TaskState.Completed, refused.Task.Status.State);
        Assert.True(refused.Task.Artifacts is null || refused.Task.Artifacts.Count == 0);
        Assert.DoesNotContain("21750.00", refusedBody);
        Assert.DoesNotContain("Recomendação demonstrativa", refusedBody);
    }

    [Fact]
    public async Task Reducao_de_20_porcento_permanece_message()
    {
        var body = await SendAsync("limite-" + Guid.NewGuid().ToString("N"), "Recomende reduzir 20% das despesas de janeiro de 2026.");
        var response = JsonSerializer.Deserialize<SendMessageResponse>(body, A2AJsonUtilities.DefaultOptions);
        Assert.NotNull(response?.Message);
        Assert.Null(response.Task);
        Assert.Contains("17400.00", body);
        Assert.Contains("Recomendação demonstrativa", body);
        Assert.DoesNotContain("TASK_STATE_INPUT_REQUIRED", body);
    }

    private async Task<string> SendAsync(string contextId, string text)
    {
        var payload = JsonSerializer.Serialize(
            new SendMessageRequest
            {
                Message = new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.User,
                    ContextId = contextId,
                    Parts = [Part.FromText(text)]
                }
            },
            A2AJsonUtilities.DefaultOptions);
        var response = await _client.PostAsync(
            "/a2a/agente-financeiro/message:send",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return body;
    }

    private string ReadCheckpoints()
    {
        if (!Directory.Exists(_factory.CheckpointDirectory))
        {
            return string.Empty;
        }

        return string.Concat(Directory.GetFiles(_factory.CheckpointDirectory, "*", SearchOption.AllDirectories).Select(File.ReadAllText));
    }
}
