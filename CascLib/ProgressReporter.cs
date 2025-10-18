using System;
using System.Threading;

namespace CASCLib;

#nullable enable

public class ProgressReporter : IProgressReporter
{
    private readonly IProgress<ProgressInfo> _progress;
    private readonly CancellationToken _token;
    private int _lastPercent = -1;

    public ProgressReporter(IProgress<ProgressInfo> progress, CancellationToken token = default)
    {
        _progress = progress;
        _token = token;
    }

    public void Start(int initialPercent, string? message = null, ProgressStage? progressStage = null)
    {
        _token.ThrowIfCancellationRequested();

        SetReport(initialPercent, message, progressStage);
    }

    public void Report(int percent, string? message = null, ProgressStage? progressStage = null)
    {
        _token.ThrowIfCancellationRequested();

        if (percent > _lastPercent)
        {
            SetReport(percent, message, progressStage);
        }
    }

    private void SetReport(int percent, string? message, ProgressStage? progressStage)
    {
        _progress.Report(new ProgressInfo(percent, message, progressStage));
        _lastPercent = percent;
    }
}
