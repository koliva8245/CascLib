namespace CASCLib;

public interface IBackgroundWorkerEx
{
    void ReportProgress(int percentProgress);
    void ReportProgress(int percentProgress, object userState);
}