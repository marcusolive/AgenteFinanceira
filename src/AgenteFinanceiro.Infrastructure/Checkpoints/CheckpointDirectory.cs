namespace AgenteFinanceiro.Infrastructure.Checkpoints;

public static class CheckpointDirectory
{
    public static string Ensure(string? configured)
    {
        var path = string.IsNullOrWhiteSpace(configured) ? "checkpoints" : configured;
        Directory.CreateDirectory(path);
        return path;
    }
}
