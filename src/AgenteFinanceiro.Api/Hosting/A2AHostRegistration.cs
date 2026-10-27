using System.Text;
using System.Text.Json;
using A2A.AspNetCore;
using AgenteFinanceiro.Application.Agent;
using AgenteFinanceiro.Application.Sessions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AgenteFinanceiro.Api.Hosting;

public static class A2AHostRegistration
{
    public static void AddServer(WebApplicationBuilder builder)
    {
        builder.Services.AddKeyedSingleton<AIAgent>(FinancialAgentFactory.AgentId, (sp, _) =>
            sp.GetRequiredService<AIAgent>());
        builder.AddA2AServer(FinancialAgentFactory.AgentId);
    }

    public static void Map(WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Value?.Contains("message:send", StringComparison.Ordinal) != true)
            {
                await next().ConfigureAwait(false);
                return;
            }

            context.Request.EnableBuffering();
            string requestBody;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            context.Request.Body.Position = 0;
            var contextId = ReadContextId(requestBody);
            using var _ = ConversationScope.Push(contextId);
            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;
            try
            {
                await next().ConfigureAwait(false);
                var responseText = Encoding.UTF8.GetString(buffer.ToArray());
                var store = context.RequestServices.GetRequiredService<InMemoryAgentSessionStore>();
                var replacement = context.Response.StatusCode == StatusCodes.Status200OK
                    ? ApprovalTaskMapper.TryRewrite(ReadText(requestBody), contextId, responseText, store)
                    : null;
                var output = Encoding.UTF8.GetBytes(replacement ?? responseText);
                context.Response.Body = originalBody;
                context.Response.ContentLength = output.Length;
                await context.Response.Body.WriteAsync(output).ConfigureAwait(false);
            }
            catch
            {
                context.Response.Body = originalBody;
                throw;
            }
        });

        app.MapA2AHttpJson(FinancialAgentFactory.AgentId, "/a2a/agente-financeiro");
        app.MapWellKnownAgentCard(AgentCardFactory.Create());
    }

    private static string ReadContextId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (document.RootElement.TryGetProperty("message", out var message) &&
                message.TryGetProperty("contextId", out var contextId))
            {
                return contextId.GetString() ?? "sem-contexto";
            }
        }
        catch (JsonException)
        {
            return "sem-contexto";
        }

        return "sem-contexto";
    }

    private static string ReadText(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (!document.RootElement.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("parts", out var parts))
            {
                return string.Empty;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }
        catch (JsonException)
        {
            return string.Empty;
        }

        return string.Empty;
    }
}
