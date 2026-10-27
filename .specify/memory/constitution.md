<!--
Sync Impact Report
- Version change: não ratificada (scaffold com placeholders) → 1.0.0
- Modified principles:
  - [PRINCIPLE_1_NAME] → I. Desenvolvimento Orientado por Especificação
  - [PRINCIPLE_2_NAME] → II. Stack Oficial
  - [PRINCIPLE_3_NAME] → III. Modelo OpenAI via IChatClient
  - [PRINCIPLE_4_NAME] → IV. Cálculo Determinístico em decimal
  - [PRINCIPLE_5_NAME] → V. Instruções Especializadas e Function Calling
  - (incluído) VI. Preservação de Contexto com AgentSession
  - (incluído) VII. Provedor de Contexto Financeiro
  - (incluído) VIII. Observabilidade sem Dados Confidenciais
  - (incluído) IX. Segredos fora do Repositório
  - (incluído) X. Workflow de Análise com Checkpoints
  - (incluído) XI. Servidor A2A Independente de SOFIA
  - (incluído) XII. Fidelidade Contábil e Dados Fictícios
  - (incluído) XIII. Testes e Documentação de Execução
- Added sections:
  - Restrições de Plataforma e Runtime (substitui [SECTION_2_NAME])
  - Fluxo de Desenvolvimento e Portões de Qualidade (substitui [SECTION_3_NAME])
- Removed sections: nenhuma (comentários de exemplo do scaffold foram removidos)
- Follow-up TODOs: nenhum
-->

# AgenteFinanceiro Constitution

## Core Principles

### I. Desenvolvimento Orientado por Especificação (NÃO NEGOCIÁVEL)

A implementação DEVE estar em conformidade com esta constituição e com
os documentos de especificação vigentes da funcionalidade (`spec.md`,
`plan.md` e `tasks.md`).

Código, configuração e testes DEVEM permanecer compatíveis com esses
documentos. Quando houver divergência, o merge espera a correção: o
código retorna à conformidade ou a especificação é emendada pelo fluxo
de governança antes de a implementação seguir o texto novo.

Precedência: esta constituição, depois `spec.md`, depois `plan.md`,
depois `tasks.md`, depois o código.

**Justificativa**: Spec-Driven Development mantém o contrato do agente,
os cálculos e a integração A2A auditáveis antes de existir código.

### II. Stack Oficial

A aplicação DEVE ser escrita em C#, em .NET 10 e ASP.NET Core.

O runtime do agente DEVE ser o Microsoft Agent Framework oficial. O
código da aplicação DEVE hospedar, configurar e compor esse runtime.
A orquestração do agente, das ferramentas, da sessão, do middleware,
do workflow e do servidor A2A DEVE usar as APIs desse framework.

**Justificativa**: Um único runtime oficial evita uma segunda pilha de
agente e acompanha as APIs suportadas pela Microsoft.

### III. Modelo OpenAI via IChatClient

O agente DEVE usar um modelo OpenAI por meio de um Model Client
compatível com `IChatClient`.

Modelo, endpoint e credencial DEVEM vir de configuração externa ao
código versionado. Testes DEVEM poder substituir o cliente por outra
implementação de `IChatClient` sem alterar ferramentas nem workflow.

**Justificativa**: `IChatClient` é o contrato do framework para o modelo
e permite exercer o agente sem depender da API real em todo teste.

### IV. Cálculo Determinístico em decimal

Toda operação matemática de análise financeira ou contabilidade
gerencial DEVE ser executada em código C#, com o tipo `decimal`.

O modelo de linguagem DEVE interpretar a solicitação, selecionar
ferramentas e redigir o texto do parecer. A fonte de verdade dos
números do parecer DEVE ser o valor devolvido pelas ferramentas.
O valor calculado e armazenado DEVE permanecer em `decimal`. A
formatação de exibição PODE convertê-lo para texto.

**Justificativa**: O modelo não é fonte de verdade aritmética. `decimal`
preserva precisão monetária e torna o resultado reproduzível em teste.

### V. Instruções Especializadas e Function Calling

O agente DEVE possuir Instructions especializadas em análise financeira
e contabilidade gerencial.

As ferramentas do agente DEVEM ser implementadas com Function Calling
do Microsoft Agent Framework. Ferramenta que produza número, indicador,
alíquota aplicada ou classificação contábil DEVE executar a regra em C#.

**Justificativa**: Instructions fixam o papel do agente. Function Calling
entrega dados e cálculos fora do texto livre do modelo.

### VI. Preservação de Contexto com AgentSession

O agente DEVE implementar `AgentSession` para preservar o contexto de
uma conversa entre turnos.

Cada conversa DEVE ter sessão própria. O contexto de uma sessão DEVE
permanecer indisponível para as demais.

**Justificativa**: A análise é multi-turno. A sessão conserva premissas
já apresentadas e o encadeamento do parecer.

### VII. Provedor de Contexto Financeiro

O agente DEVE implementar um provedor de contexto financeiro que
forneça, durante a solicitação, os dados relevantes à análise.

O provedor DEVE entregar somente dados presentes nas fontes aprovadas:
demonstrativos fictícios, fixtures versionadas e entradas explícitas da
solicitação. O parecer DEVE usar esses dados e os resultados das
ferramentas como base fática.

**Justificativa**: O modelo recebe os demonstrativos no turno, no lugar
de reconstruir livros contábeis a partir do próprio treinamento.

### VIII. Observabilidade sem Dados Confidenciais

Um middleware DEVE registrar as chamadas relevantes do agente, o tempo
de execução e as exceções.

O registro DEVE incluir correlação, nome da operação, duração, desfecho
(sucesso ou falha) e o tipo da exceção. O registro DEVE omitir dados
confidenciais: chaves de API, segredos, tokens, connection strings,
conteúdo de demonstrativos, documentos e prompts com payload financeiro.
Mensagem de exceção persistida DEVE ser sanitizada antes do armazenamento.

**Justificativa**: Duração e falha são necessárias para operar o agente.
O conteúdo financeiro e os segredos ficam fora dos logs.

### IX. Segredos fora do Repositório

Chaves de API, tokens, connection strings e demais segredos DEVEM
permanecer fora do repositório. Isso inclui código-fonte, testes,
fixtures, documentação e histórico de commits.

A configuração local DEVE usar mecanismo externo ao controle de versão,
como User Secrets, variáveis de ambiente ou cofre. Exemplos versionados
DEVEM listar apenas nomes de chaves, sem valores. Na ausência da
credencial do modelo, a aplicação DEVE falhar de forma explícita.

**Justificativa**: Segredo versionado permanece no histórico do Git e
não pode ser tratado como configuração descartável.

### X. Workflow de Análise com Checkpoints

A análise financeira DEVE ser um Workflow do Microsoft Agent Framework
com as etapas de validação, processamento, geração de parecer e
aprovação humana.

A etapa de aprovação humana DEVE executar quando as regras da
especificação exigirem decisão humana. Quando a especificação definir
que o caso dispensa aprovação, a dispensa DEVE ficar explícita no
fluxo. A aprovação DEVE ser uma decisão registrada por um ator humano.
Checkpoints DEVEM permitir continuar o fluxo a partir da aprovação
pendente, conservando o estado já produzido pelas etapas anteriores.

**Justificativa**: O parecer sujeito a aprovação humana ultrapassa uma
única requisição. O checkpoint torna a continuidade do fluxo auditável.

### XI. Servidor A2A Independente de SOFIA

A aplicação DEVE disponibilizar um servidor A2A com Agent Card e Skills
que descrevam as capacidades do agente.

O agente DEVE ser executável, testável e publicável por conta própria.
Compilação, configuração obrigatória e execução DEVEM ser possíveis na
ausência do orquestrador SOFIA. A integração futura com SOFIA DEVE
ocorrer por A2A, com SOFIA no papel de cliente externo.

**Justificativa**: O contrato de integração é o protocolo A2A. O agente
permanece um serviço autônomo até a existência do orquestrador.

### XII. Fidelidade Contábil e Dados Fictícios

Desenvolvimento, teste e demonstração DEVEM usar dados financeiros
fictícios e demonstrativos versionados no repositório ou fornecidos de
forma explícita na solicitação.

Valor, alíquota, documento e informação contábil DEVEM constar da fonte
aprovada antes de aparecerem em ferramenta, contexto ou parecer. Dado
ausente DEVE ser reportado como lacuna da análise. Uso de livro contábil
real exige emenda desta constituição.

**Justificativa**: Dados fictícios fixos tornam o teste reproduzível.
Lacuna explícita impede que o parecer complete a contabilidade com
informação inexistente na fonte.

### XIII. Testes e Documentação de Execução

O repositório DEVE conter testes unitários e testes de integração, e
DEVE documentar como executar a aplicação e como executar os testes.

Os testes unitários DEVEM cobrir cálculos em `decimal`, ferramentas,
transições do workflow, retomada por checkpoint, o provedor de contexto
e a sanitização de logs do middleware.

Os testes de integração DEVEM cobrir o host ASP.NET Core, o Agent Card,
as Skills do servidor A2A e a continuidade de conversa via `AgentSession`.

A documentação DEVE incluir a configuração local sem gravar segredos e
o comando de execução dos dois níveis de teste. Ambientes Development,
Test e Production DEVEM ler configuração pelo mecanismo do ASP.NET Core.

**Justificativa**: Cálculo, workflow e contrato A2A permanecem confiáveis
somente com teste automatizado e instruções reproduzíveis.

## Restrições de Plataforma e Runtime

- Linguagem e host: C#, .NET 10, ASP.NET Core.
- Runtime do agente: Microsoft Agent Framework oficial.
- Modelo: OpenAI, por Model Client compatível com `IChatClient`.
- Números financeiros: `decimal` nas ferramentas e no domínio de cálculo.
- Conversa: `AgentSession` isolada por conversa.
- Dados do turno: provedor de contexto financeiro.
- Observabilidade: middleware de chamada, duração e exceção, com
  sanitização e sem payload confidencial.
- Análise: Workflow com validação, processamento, parecer e aprovação
  humana, com Checkpoints na espera dessa aprovação.
- Borda externa: servidor A2A, Agent Card e Skills. SOFIA permanece fora
  do processo do agente.
- Massa de dados: demonstrativos e séries fictícias. Lacuna de dado
  encerra o item correspondente com erro explícito da ferramenta.
- Segredos: User Secrets, variáveis de ambiente ou cofre. O repositório
  DEVE ignorar arquivos que contenham segredo.

## Fluxo de Desenvolvimento e Portões de Qualidade

1. A funcionalidade começa pela especificação. Plano e tarefas derivam
   dela. Implementação sem tarefa rastreável na spec vigente fica de
   fora do produto.
2. Antes do merge, a revisão DEVE checar os princípios I a XIII:
   Spec-Driven Development, stack, `IChatClient`, `decimal`, Function
   Calling, `AgentSession`, provedor de contexto, middleware sanitizado,
   segredos fora do repositório, workflow com checkpoint, A2A sem
   dependência de SOFIA, dados fictícios, testes unitários e de
   integração, e documentação de execução e teste.
3. A suíte DEVE falhar quando o cálculo financeiro deixar de usar
   `decimal`, quando o parecer apresentar número ausente nas ferramentas
   e no contexto, quando o log armazenar segredo ou payload financeiro,
   ou quando o host passar a exigir SOFIA para iniciar.
4. A revisão DEVE recusar chave, token ou connection string em fonte,
   teste, fixture ou documentação.
5. Exceção a um princípio DEVE ser emenda desta constituição. Comentário
   no código não altera a regra.

## Governance

Esta constituição prevalece sobre especificações de funcionalidade,
planos, tarefas, templates do Spec Kit e convenções locais. A precedência
completa está no Princípio I.

Verbos normativos: **DEVE** tem a força de MUST. A forma negativa direta
nas proibições dos princípios tem a força de MUST NOT. **DEVERIA** tem a
força de SHOULD e só cabe quando a própria constituição admitir exceção
justificada na especificação. **PODE** tem a força de MAY.

Emendas:

- Alteração de princípio exige atualização deste arquivo, nova versão
  semântica e nova data de emenda.
- MAJOR: remoção ou redefinição incompatível de princípio.
- MINOR: princípio novo ou expansão material de orientação.
- PATCH: esclarecimento, correção de redação ou ajuste sem mudança de
  regra.
- A data de ratificação original permanece. A data da última emenda muda
  a cada alteração aceita.
- A emenda DEVE descrever, no histórico abaixo, o que mudou e por quê.
- O comentário Sync Impact Report no topo é rascunho de revisão humana e
  DEVE ser removido antes do commit da emenda.

Revisão de conformidade:

- Todo plano de funcionalidade e toda revisão de mudança DEVE checar os
  princípios I a XIII.
- Capacidade além do workflow, das ferramentas e do servidor A2A
  descritos aqui DEVE ser justificada na especificação da funcionalidade.
- Runtime guidance para implementação é este arquivo, lido junto dos
  artefatos em `specs/` no momento do planejamento.

**Histórico de versão**

- **1.0.0** (2026-10-27): Ratificação inicial. Estabelece Spec-Driven
  Development, C# em .NET 10 e ASP.NET Core com o Microsoft Agent
  Framework oficial, modelo OpenAI via `IChatClient`, cálculo em
  `decimal`, Instructions com Function Calling, `AgentSession`, provedor
  de contexto financeiro, middleware sem dados confidenciais, segredos
  fora do repositório, workflow com checkpoints e aprovação humana,
  servidor A2A independente de SOFIA, dados fictícios com lacuna explícita
  no lugar de informação contábil inexistente, testes unitários e de
  integração, e documentação de execução e teste.

**Version**: 1.0.0 | **Ratified**: 2026-10-27 | **Last Amended**: 2026-10-27
