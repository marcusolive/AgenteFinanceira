using A2A;
using Microsoft.Agents.AI;

var resolver = new A2ACardResolver(new Uri("http://localhost:5100"));
AgentCard card;
try
{
    card = await resolver.GetAgentCardAsync();
}
catch (Exception exception)
{
    Console.WriteLine($"Falha de descoberta do Agent Card: {exception.GetType().Name}. Nenhuma resposta financeira foi fabricada.");
    return;
}

Console.WriteLine(card.Name);
var agent = await resolver.GetAIAgentAsync();
var session = await agent.CreateSessionAsync();
var response = await agent.RunAsync("Qual o fluxo de caixa de janeiro de 2026?", session);
Console.WriteLine(response.Text);
