using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace AgenteFinanceiro.Api.Hosting;

public static class OpenAIChatClientRegistration
{
    public static IChatClient Create(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Configuração ausente: OpenAI:ApiKey. Defina User Secrets ou a variável OpenAI__ApiKey.");
        }

        var model = configuration["OpenAI:Model"];
        if (string.IsNullOrWhiteSpace(model))
        {
            model = Environment.GetEnvironmentVariable("OpenAI__Model");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "Configuração ausente: OpenAI:Model. Defina User Secrets ou a variável OpenAI__Model.");
        }

        return new OpenAIClient(new ApiKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient();
    }
}
