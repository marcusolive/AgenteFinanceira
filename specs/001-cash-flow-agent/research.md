# Research: AgenteFinanceiro

Pesquisa da API pública em 2026-10-27. Fonte principal: Microsoft Learn
(`agent-framework-dotnet-latest`) e NuGet, pacotes publicados em
2026-10-23.

## 1. Linha de pacotes

- **Decision**: Fixar `Microsoft.Agents.AI` 1.26.0,
  `Microsoft.Agents.AI.OpenAI` 1.26.0 e `Microsoft.Agents.AI.Workflows`
  1.26.0. Fixar os pacotes A2A ainda em pré-lançamento na mesma linha:
  `Microsoft.Agents.AI.Hosting.A2A.AspNetCore` 1.26.0-preview.261023.1 e
  `Microsoft.Agents.AI.A2A` 1.26.0-preview.261023.1. O metapacote de
  hospedagem traz `A2A` e `A2A.AspNetCore` >= 1.0.0-preview3.
- **Rationale**: 1.26.0 é a versão estável atual do framework e declara
  compatibilidade com `net10.0`. O binding ASP.NET Core de A2A ainda não
  saiu da faixa preview nessa linha; é o pacote que a documentação atual
  de hospedagem manda instalar.
- **Alternatives considered**: A página antiga de hosting mostra
  `MapA2A` e `GET /a2a/{agent}/v1/card`. A página atual "Host agents with
  A2A" e "A2A Hosting (.NET)" mostram `MapA2AHttpJson` e
  `/.well-known/agent-card.json`. O plano segue a página atual.

## 2. IChatClient e OpenAI

- **Decision**: Na Api, criar o cliente com o SDK oficial OpenAI e a
  extensão do framework:

```csharp
IChatClient chatClient = new OpenAIClient(new ApiKeyCredential(apiKey))
    .GetChatClient(model)
    .AsIChatClient();
```

  `AsIChatClient` vem de `Microsoft.Agents.AI.OpenAI` /
  `Microsoft.Extensions.AI.OpenAI` 10.10.1, sobre `OpenAI` >= 2.14.0.
  Registrar `IChatClient` no contêiner. Testes substituem esse registro.
- **Rationale**: A constituição exige OpenAI por um Model Client
  compatível com `IChatClient`. A Api é o único projeto que conhece a
  credencial.
- **Alternatives considered**: Azure OpenAI e Microsoft Foundry aparecem
  nos exemplos oficiais. A spec desta aula fixa o provedor OpenAI, então
  Foundry fica de fora. Chamar HTTP direto no endpoint de chat foi
  rejeitado porque contornaria `IChatClient`.

## 3. AIAgent e ChatClientAgent

- **Decision**: O agente especializado é um `ChatClientAgent`, subclasse
  concreta de `AIAgent`, no namespace `Microsoft.Agents.AI`.

```csharp
AIAgent agent = new ChatClientAgent(
    chatClient,
    new ChatClientAgentOptions
    {
        Id = "agente-financeiro",
        Name = "AgenteFinanceiro",
        Description = "Análise de fluxo de caixa da base demonstrativa Aurora.",
        ChatOptions = new ChatOptions
        {
            Instructions = instructions,
            Tools = tools
        },
        AIContextProviders = [financialContextProvider]
    });
```

  `Id` e `Name` permanecem estáveis para o checkpoint do workflow poder
  ser retomado. O framework envolve o `IChatClient` com function calling
  por padrão. `UseProvidedChatClientAsIs` fica desligado.
- **Rationale**: `ChatClientAgent` é o `AIAgent` que delega a um
  `IChatClient`. Instructions, ferramentas e provedores de contexto são
  opções desse tipo. `RunAsync(string, AgentSession, CancellationToken)`
  e `CreateSessionAsync` estão em `AIAgent`.
- **Alternatives considered**: `chatClient.AsAIAgent(options)` é atalho
  equivalente quando o cliente já é `IChatClient`. O construtor explícito
  deixa Id, Instructions, Tools e providers visíveis na composição. Um
  agente escrito à mão, sem `ChatClientAgent`, foi rejeitado.

## 4. Tools e Function Calling

- **Decision**: Métodos C# do projeto Application são embrulhados com
  `AIFunctionFactory.Create(...)` (`Microsoft.Extensions.AI`) e colocados
  em `ChatOptions.Tools` como `AITool` / `AIFunction`. O cálculo fica no
  Domain. A ferramenta só adapta a chamada.
- **Rationale**: É o mecanismo público de function calling do
  `IChatClient`. O agente passa a poder invocar o método com argumentos
  gerados pelo modelo, e o valor devolvido é o resultado C#.
- **Alternatives considered**: `AIAgent.AsAIFunction()` serve para expor
  outro agente como ferramenta. Aqui as ferramentas são cálculos
  determinísticos, não outro agente.

## 5. AgentSession

- **Decision**: Cada conversa A2A usa uma `AgentSession` criada por
  `agent.CreateSessionAsync()`. Os turnos seguintes chamam
  `agent.RunAsync(mensagem, session)`. O `contextId` do protocolo A2A é
  a chave da conversa. O mapa `contextId` → sessão vive em memória no
  processo. `SerializeSessionAsync` / `DeserializeSessionAsync` não são
  usados para durar além do processo.
- **Rationale**: A spec pede sessão em memória nesta versão. A
  documentação de A2A trata `contextId` como identificador da conversa e
  mostra `CreateSessionAsync` mais `RunAsync` no cliente. Reiniciar o
  processo descarta o mapa.
- **Alternatives considered**: Persistir a sessão em JSON foi rejeitado
  porque a spec separa sessão (memória) de checkpoint de aprovação
  (arquivo). Reusar o identificador de serviço da OpenAI como autorização
  de usuário foi rejeitado; esta aula não tem vários usuários.

## 6. AIContextProvider

- **Decision**: `FinancialContextProvider : AIContextProvider` sobrescreve
  `ProvideAIContextAsync(InvokingContext, CancellationToken)`. O contexto
  acrescentado lista a empresa fictícia Aurora, os meses disponíveis e as
  categorias de despesa, e declara que a base é somente leitura. Não
  inclui valores monetários nem credencial.
- **Rationale**: A classe abstrata em `Microsoft.Agents.AI.Abstractions`
  participa do início da invocação por `InvokingAsync` /
  `ProvideAIContextAsync` e pode observar o fim por `InvokedAsync`. O
  catálogo ajuda o modelo a não inventar mês ou categoria. Os valores
  continuam saindo das ferramentas, que são a fonte de verdade.
- **Alternatives considered**: Colocar a tabela inteira no prompt foi
  rejeitado porque o parecer poderia citar número sem ferramenta.
  Consultar um banco para montar o contexto foi rejeitado pela spec.

## 7. Middleware

- **Decision**: Depois de criar o agente, aplicar middleware de execução
  com o builder público:

```csharp
AIAgent agent = chatClientAgent
    .AsBuilder()
    .Use(runFunc: ExecutionLogMiddleware, runStreamingFunc: ExecutionLogStreamingMiddleware)
    .Build();
```

  O middleware mede o tempo, registra o nome `AgenteFinanceiro`, o
  identificador da sessão, o desfecho e o tipo da exceção. Não registra
  o texto da pergunta, o demonstrativo, a chave nem o connection string.
  Em falha, a exceção segue depois do registro sanitizado.
- **Rationale**: A documentação "Agent Middleware" define três camadas:
  agent run (`AIAgentBuilder.Use`), function calling e `IChatClient`
  (`AsBuilder().Use(getResponseFunc, getStreamingResponseFunc)`). O log
  da aula é da execução inteira, então fica na camada do agente. As duas
  funções, síncrona de stream e não, são informadas para o streaming não
  ser rebaixado em silêncio.
- **Alternatives considered**: Middleware só no `IChatClient` não vê o
  tempo total com ferramentas e workflow. Logar o `ChatMessage` foi
  rejeitado pela constituição.

## 8. Workflow, aprovação e checkpoint

- **Decision**: Cada solicitação entra num `Workflow` de
  `Microsoft.Agents.AI.Workflows` com executores de validação, cálculo e
  parecer. A aprovação humana é um `RequestPort`:

```csharp
RequestPort approvalPort = RequestPort.Create<ApprovalRequest, ApprovalDecision>("AprovacaoRecomendacao");
```

  O host observa `RequestInfoEvent` e responde com
  `request.Request.CreateResponse(decision)` e `run.SendResponseAsync`.
  Redução de até 20%, inclusive 20%, não envia mensagem a esse portão.

  Checkpoint:

```csharp
using FileSystemJsonCheckpointStore store = new(checkpointDirectory);
CheckpointManager checkpointManager = CheckpointManager.CreateJson(store);
StreamingRun run = await InProcessExecution.RunStreamingAsync(
    workflow, input, checkpointManager);
```

  Retomada depois do processo, para a pendência já gravada:

```csharp
StreamingRun resumed = await InProcessExecution.ResumeStreamingAsync(
    workflow, savedCheckpoint, checkpointManager);
```

  O diretório é local, de demonstração, e não entra no Git. O arquivo não
  recebe credencial nem o texto integral da conversa. O `Id` do
  `ChatClientAgent` permanece `agente-financeiro` em toda reconstrução.
- **Rationale**: A documentação de checkpoints em C# usa `CheckpointManager`
  e `InProcessExecution`. `FileSystemJsonCheckpointStore` é a
  implementação oficial que persiste JSON em disco
  (`Microsoft.Agents.AI.Workflows.Checkpointing`). `CreateJson(store)` é
  o factory usado no sample oficial de checkpoint JSON. O `RequestPort` é
  o mecanismo documentado de human-in-the-loop. A spec pede exatamente
  essa combinação: sessão em memória e checkpoint de aprovação em arquivo.
- **Alternatives considered**: `CheckpointManager.CreateInMemory()` não
  sobrevive ao processo. Um arquivo JSON escrito à mão, fora do
  `ICheckpointStore`, não retomaria `ResumeStreamingAsync`. Banco para o
  checkpoint foi rejeitado pela spec.

## 9. Hospedagem A2A e Agent Card

- **Decision**: Na Api, registrar o agente por chave e publicar HTTP+JSON
  mais o cartão well-known. Trecho alinhado à documentação atual:

```csharp
builder.Services.AddKeyedSingleton<AIAgent>("agente-financeiro", (sp, _) => composedAgent);
builder.AddA2AServer("agente-financeiro");

app.MapA2AHttpJson("agente-financeiro", "/a2a/agente-financeiro");
app.MapWellKnownAgentCard(agentCard);
app.MapHealthChecks("/health");
```

  O cartão (`A2A.AgentCard`) leva `Name`, `Description`, `Version` e
  `SupportedInterfaces` com `AgentInterface.Url` em
  `http://localhost:5100/a2a/agente-financeiro`,
  `ProtocolBindingNames.HttpJson` e `ProtocolVersion` `1.0`. As seis
  capacidades da spec entram na coleção de skills do `AgentCard`, no
  campo que o SDK serializa como `skills`. A URL do cartão é
  `GET http://localhost:5100/.well-known/agent-card.json`. A mensagem é
  `POST http://localhost:5100/a2a/agente-financeiro/message:send`.
- **Rationale**: `MapWellKnownAgentCard` pertence a `A2A.AspNetCore`.
  `AddA2AServer` e `MapA2AHttpJson` pertencem ao pacote de hosting do
  framework. HTTP+JSON é o binding preferido pelo cliente oficial. Saúde
  usa `AddHealthChecks` e `MapHealthChecks`, da plataforma ASP.NET Core,
  fora do protocolo A2A.
- **Alternatives considered**: JSON-RPC (`MapA2AJsonRpc`) pode ser ligado
  depois. Esta aula publica um binding, HTTP+JSON, para o simulador ter
  um alvo só. O cartão antigo em `/v1/card` não é o well-known URI que
  `A2ACardResolver` consulta.

## 10. Cliente A2A do Sofia.Simulator

- **Decision**: O console referencia `Microsoft.Agents.AI.A2A` e usa o
  SDK oficial:

```csharp
A2ACardResolver resolver = new(new Uri("http://localhost:5100"));
AIAgent remote = await resolver.GetAIAgentAsync();
AgentSession session = await remote.CreateSessionAsync();
await remote.RunAsync(
    "Qual o fluxo de caixa de janeiro de 2026?",
    session);
```

  A descoberta padrão lê `/.well-known/agent-card.json`.
  `GetAgentCardAsync()` existe quando o simulador precisar imprimir o
  cartão antes da mensagem. `A2AClientOptions.PreferredBindings` pode
  fixar `ProtocolBindingNames.HttpJson`. O processo imprime o texto da
  resposta no console. Não há referência de projeto à Api.
- **Rationale**: A documentação "A2A agent service" define
  `A2ACardResolver.GetAIAgentAsync()` como a descoberta pelo well-known
  URI e devolve um `AIAgent` (`A2AAgent`). Isso torna a chamada A2A real,
  não uma chamada em processo ao `ChatClientAgent`.
- **Alternatives considered**: `new A2AClient(uri).AsAIAgent(...)` pula a
  descoberta do cartão. `HttpClient` artesanal imitaria o protocolo e
  violaria o pedido de SDK oficial.

## 11. Message, Task e Artifact no mesmo `message:send`

- **Decision**: A aula usa os três objetos do protocolo A2A sem um
  segundo ciclo de vida. A resposta imediata (fluxo de caixa, lacuna,
  parecer e redução de até 20%) é uma `Message` com partes de texto.
  Quando a redução é superior a 20%, o mesmo `POST .../message:send`
  devolve uma `Task` com estado `input-required` e que ainda não
  tem `Artifact`. O identificador dessa Task é o identificador do
  checkpoint do workflow. Depois de `aprovo` no mesmo `contextId`, a
  retomada marca a Task como `completed` e anexa um único `Artifact` de texto com
  `Recomendação demonstrativa` e o valor calculado em `decimal`. Recusa
  conclui a Task sem esse Artifact e sem o valor. Os tipos vêm do
  pacote `A2A` (transitivo de `Microsoft.Agents.AI.A2A`). Se a classe
  `Task` colidir com `System.Threading.Tasks.Task`, usar alias. Não
  implementar `tasks/send`, `tasks/get`, streaming de Artifact, push
  notification nem um repositório de Artifact.
- **Rationale**: O material cita Message, Task e Artifact. O
  `MapA2AHttpJson` desta versão já entrega a Message síncrona. A Task
  só existe enquanto o `RequestPort` de Human-in-the-Loop está aberto,
  e o Artifact só existe quando o C# libera a frase. Isso cabe na
  demonstração de quatro horas porque reaproveita o checkpoint e o
  endpoint já previstos.
- **Alternatives considered**: Uma fila de tasks com polling foi
  rejeitada porque duplicaria o checkpoint e estouraria a aula. Omitir
  Task e Artifact deixaria o material sem cobertura. Tratar todo
  demonstrativo como Artifact foi rejeitado porque a consulta factual
  já está especificada como Message.

## 12. Dados, sessão e MCP

- **Decision**: `InMemoryFinancialRepository` devolve somente as tabelas
  da spec. Não há `DbContext` nem servidor de banco. Não há pacote MCP.
  A lista `ChatOptions.Tools` é o ponto em que uma integração MCP futura
  acrescentaria `AITool`, sem esta aula subir um servidor MCP.
- **Rationale**: A spec proíbe banco e servidor MCP e exige a base
  fictícia em memória, somente leitura.
- **Alternatives considered**: SQLite local foi rejeitado. Um servidor
  MCP vazio foi rejeitado porque a spec diz que esta aula não o
  implementa.

## 13. Testes sem OpenAI e com A2A real

- **Decision**: Testes de cálculo e de ferramenta usam o Domain e um
  repositório em memória, sem `IChatClient`. Testes de aprovação exercem
  o workflow com `RequestPort` e um diretório temporário de
  `FileSystemJsonCheckpointStore`. O teste de integração sobe a Api com
  `WebApplicationFactory`, troca o `IChatClient` por uma implementação de
  teste e verifica `GET /.well-known/agent-card.json` e
  `POST /a2a/agente-financeiro/message:send`. O teste
  `A2AApprovalTaskTests` cobre a Task sem Artifact e, depois da
  aprovação, o Artifact de texto. O quickstart é o cenário
  com OpenAI de verdade.
- **Rationale**: A spec pede A2A real e também proíbe inventar números.
  O cliente de teste continua sendo o protocolo A2A; só o modelo é
  substituído, que é o que a constituição permite ao exigir `IChatClient`
  substituível.
- **Alternatives considered**: Integração sempre contra a API OpenAI
  tornaria o teste lento, pago e instável. Mock que chama o método C# do
  agente sem HTTP não provaria o cartão nem o `message:send`.
