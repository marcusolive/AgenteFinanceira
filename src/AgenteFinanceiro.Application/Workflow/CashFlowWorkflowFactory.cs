using AgenteFinanceiro.Application.Analysis;
using AgenteFinanceiro.Domain.Finance;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace AgenteFinanceiro.Application.Workflow;

public sealed class ApprovalRequest
{
    public required string CheckpointId { get; init; }

    public required int Year { get; init; }

    public required int Month { get; init; }

    public decimal? Percent { get; init; }

    public decimal ExpenseBase { get; init; }

    public required string State { get; init; }
}

public sealed class ApprovalDecision
{
    public required bool Approved { get; init; }
}

public sealed class CashFlowWorkflowFactory
{
    private readonly ValidationExecutor _validation;
    private readonly CalculationExecutor _calculation;
    private readonly OpinionExecutor _opinion;
    private readonly string _checkpointRoot;

    public CashFlowWorkflowFactory(
        ValidationExecutor validation,
        CalculationExecutor calculation,
        OpinionExecutor opinion,
        string checkpointRoot)
    {
        _validation = validation;
        _calculation = calculation;
        _opinion = opinion;
        _checkpointRoot = checkpointRoot;
    }

    public RequestPort ApprovalPort { get; } = RequestPort.Create<ApprovalRequest, ApprovalDecision>("AprovacaoRecomendacao");

    public Exception? LastWorkflowError { get; private set; }

    public CheckpointInfo? SavedCheckpoint { get; private set; }

    public async Task<AnalysisState> RunAsync(string question, ConversationSession? session, CancellationToken cancellationToken)
    {
        var state = QuestionInterpreter.Interpret(question, session?.ContextId ?? "sem-contexto");
        state = _validation.Execute(state, session);
        state = _calculation.Execute(state);
        if (state.AsksForApproval && state.Percent is > 20m)
        {
            state.CheckpointId ??= Guid.NewGuid().ToString("N");
        }

        try
        {
            if (state.Kind is AnalysisKind.Approval or AnalysisKind.Rejection && SavedCheckpoint is not null)
            {
                await ResumeAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (state.AsksForApproval && state.Percent is > 20m)
            {
                await PersistApprovalAsync(state, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await RunPipelineAsync(state, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            LastWorkflowError = exception;
        }

        if (session is not null && state.AsksForApproval && state.Percent is > 20m)
        {
            session.ApprovalCheckpointId ??= ReadLatestCheckpointId() ?? state.CheckpointId;
        }

        return _opinion.Execute(state, session);
    }

    public Microsoft.Agents.AI.Workflows.Workflow Build(AnalysisState state)
    {
        var validation = new FunctionExecutor<AnalysisState, AnalysisState>(
            "validacao",
            (input, _, _) => ValueTask.FromResult(_validation.Execute(input, null)),
            null,
            [typeof(AnalysisState)],
            [typeof(AnalysisState)],
            true);
        var calculation = new FunctionExecutor<AnalysisState, AnalysisState>(
            "calculo",
            (input, _, _) => ValueTask.FromResult(_calculation.Execute(input)),
            null,
            [typeof(AnalysisState)],
            [typeof(AnalysisState)],
            true);
        var opinion = new FunctionExecutor<AnalysisState, AnalysisState>(
            "parecer",
            (input, context, token) => PublishAsync(input, context, token),
            null,
            [typeof(AnalysisState)],
            [typeof(AnalysisState)],
            true);

        var builder = new WorkflowBuilder(validation)
            .AddEdge(validation, calculation)
            .AddEdge(calculation, opinion)
            .WithOutputFrom(opinion);

        if (state.Percent is > 20m)
        {
            builder.BindExecutor(ApprovalPort);
            builder.AddEdge(opinion, ApprovalPort);
        }

        return builder.Build();
    }

    private async ValueTask<AnalysisState> PublishAsync(AnalysisState input, IWorkflowContext context, CancellationToken cancellationToken)
    {
        await context.YieldOutputAsync(input, cancellationToken).ConfigureAwait(false);
        if (input.Percent is > 20m)
        {
            await context.SendMessageAsync(new ApprovalRequest
            {
                CheckpointId = input.CheckpointId ?? Guid.NewGuid().ToString("N"),
                Year = input.Year ?? 0,
                Month = input.Month ?? 0,
                Percent = input.Percent,
                ExpenseBase = input.ExpenseBase,
                State = ApprovalState.Pending.ToString()
            }, ApprovalPort.Id, cancellationToken).ConfigureAwait(false);
        }

        return input;
    }

    private async Task RunPipelineAsync(AnalysisState state, CancellationToken cancellationToken)
    {
        var directory = Directory.CreateDirectory(_checkpointRoot).FullName;
        using var store = new FileSystemJsonCheckpointStore(new DirectoryInfo(directory));
        var manager = CheckpointManager.CreateJson(store);
        var workflow = Build(state);
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, manager, null, cancellationToken).ConfigureAwait(false);
        await foreach (var _ in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
        }
    }

    private async Task PersistApprovalAsync(AnalysisState state, CancellationToken cancellationToken)
    {
        var directory = Directory.CreateDirectory(_checkpointRoot).FullName;
        using var store = new FileSystemJsonCheckpointStore(new DirectoryInfo(directory));
        var manager = CheckpointManager.CreateJson(store);
        var workflow = Build(state);
        await using var run = await InProcessExecution.RunStreamingAsync(workflow, state, manager, null, cancellationToken).ConfigureAwait(false);
        await foreach (var workflowEvent in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            if (workflowEvent is SuperStepCompletedEvent completed && completed.CompletionInfo?.Checkpoint is not null)
            {
                SavedCheckpoint = completed.CompletionInfo.Checkpoint;
            }

            if (workflowEvent is RequestInfoEvent)
            {
                break;
            }
        }
    }

    private string? ReadLatestCheckpointId()
    {
        if (SavedCheckpoint?.CheckpointId is { Length: > 0 } known)
        {
            return known;
        }

        if (!Directory.Exists(_checkpointRoot))
        {
            return null;
        }

        var index = Directory.GetFiles(_checkpointRoot, "index.jsonl", SearchOption.AllDirectories).FirstOrDefault();
        if (index is null)
        {
            return null;
        }

        string? found = null;
        foreach (var line in File.ReadLines(index))
        {
            found = ReadCheckpointId(line) ?? found;
        }

        return found;
    }

    private static string? ReadCheckpointId(string line)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("checkpointInfo", out var info) &&
                info.TryGetProperty("checkpointId", out var checkpointId))
            {
                return checkpointId.GetString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }

        return null;
    }

    private async Task ResumeAsync(CancellationToken cancellationToken)
    {
        if (SavedCheckpoint is null)
        {
            return;
        }

        var directory = Directory.CreateDirectory(_checkpointRoot).FullName;
        using var store = new FileSystemJsonCheckpointStore(new DirectoryInfo(directory));
        var manager = CheckpointManager.CreateJson(store);
        var workflow = Build(new AnalysisState { ContextId = "retomada" });
        await using var run = await InProcessExecution.ResumeStreamingAsync(workflow, SavedCheckpoint, manager, cancellationToken).ConfigureAwait(false);
        var seen = 0;
        await foreach (var workflowEvent in run.WatchStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            seen++;
            if (workflowEvent is RequestInfoEvent request)
            {
                var response = request.Request.CreateResponse(new ApprovalDecision { Approved = true });
                await run.SendResponseAsync(response).ConfigureAwait(false);
                break;
            }

            if (seen > 8)
            {
                break;
            }
        }
    }
}
