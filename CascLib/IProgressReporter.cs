namespace CASCLib;

#nullable enable

public interface IProgressReporter
{
    void Report(int percent, string? message = null);

    void Start(int initialPercent, string? message = null);
}
