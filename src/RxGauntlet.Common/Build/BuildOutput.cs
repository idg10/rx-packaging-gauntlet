namespace RxGauntlet.Build;

public record BuildOutput(
    int BuildProcessExitCode,
    string OutputFolder)
{
    public bool Succeeded => BuildProcessExitCode == 0;
}
