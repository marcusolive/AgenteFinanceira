using System.Text.Json;
using A2A;
using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Application.Sessions;
using AgenteFinanceiro.Domain.Finance;

namespace AgenteFinanceiro.Api.Hosting;

public static class ApprovalTaskMapper
{
    public static string? TryRewrite(string question, string contextId, string responseJson, InMemoryAgentSessionStore store)
    {
        if (!store.TryGet(contextId, out var entry) || entry?.Conversation.ApprovalCheckpointId is not { Length: > 0 } checkpointId)
        {
            return null;
        }

        var kind = QuestionInterpreter.Interpret(question, contextId).Kind;
        var decision = entry.Conversation.PendingApproval?.Decision;
        var agentText = ReadAgentText(responseJson);
        var task = kind switch
        {
            AnalysisKind.Reduction or AnalysisKind.VagueReduction or AnalysisKind.Ambiguous when decision == ApprovalState.Pending =>
                CreateTask(checkpointId, contextId, TaskState.InputRequired, agentText, includeArtifact: false),
            AnalysisKind.Approval when decision == ApprovalState.Approved =>
                CreateTask(checkpointId, contextId, TaskState.Completed, agentText, includeArtifact: true),
            AnalysisKind.Rejection when decision == ApprovalState.Rejected =>
                CreateTask(checkpointId, contextId, TaskState.Completed, agentText, includeArtifact: false),
            _ => null
        };

        if (task is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new SendMessageResponse { Task = task }, A2AJsonUtilities.DefaultOptions);
    }

    private static AgentTask CreateTask(string checkpointId, string contextId, TaskState state, string text, bool includeArtifact)
    {
        var task = new AgentTask
        {
            Id = checkpointId,
            ContextId = contextId,
            Status = new A2A.TaskStatus
            {
                State = state,
                Message = new Message
                {
                    Role = Role.Agent,
                    MessageId = Guid.NewGuid().ToString("N"),
                    ContextId = contextId,
                    Parts = [Part.FromText(text)]
                }
            }
        };

        if (includeArtifact)
        {
            task.Artifacts =
            [
                new Artifact
                {
                    ArtifactId = Guid.NewGuid().ToString("N"),
                    Name = "recomendacao-demonstrativa",
                    Parts = [Part.FromText(text)]
                }
            ];
        }

        return task;
    }

    private static string ReadAgentText(string responseJson)
    {
        try
        {
            var response = JsonSerializer.Deserialize<SendMessageResponse>(responseJson, A2AJsonUtilities.DefaultOptions);
            return response?.Message?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
