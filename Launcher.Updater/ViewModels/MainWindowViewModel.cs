using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Updater.Models;

namespace Launcher.Updater.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Cuda Launcher — Updater";

    [ObservableProperty]
    private string _statusTitle = "Downloading Update...";

    [ObservableProperty]
    private string _statusDetail = "Preparing for update...";

    [ObservableProperty]
    private string _speedInfo = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PercentText))]
    private double _progress;

    [ObservableProperty]
    private bool _isIndeterminate;

    public string PercentText => $"{(int)Math.Round(Progress)}%";
    
    public Progress<UpdateProgressInfo> ProgressReporter { get; }

    public MainWindowViewModel() : this(new Progress<UpdateProgressInfo>())
    {
    }

    public MainWindowViewModel(Progress<UpdateProgressInfo> progress)
    {
        ProgressReporter = progress;
        progress.ProgressChanged += (_, args) =>
        {
            StatusTitle = args.StatusTitle;
            StatusDetail = args.StatusDetails;
            SpeedInfo = args.DownloadSpeed ?? string.Empty;
            Progress = args.Progress;
            IsIndeterminate = args.Progress < 0;
        };
    }
}
