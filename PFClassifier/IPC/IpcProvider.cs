using Dalamud.Plugin.Ipc;
using PFClassifier.Classifier;
using PFClassifier.Models;

namespace PFClassifier.IPC;

public class IpcProvider : IDisposable
{
    private const string IpcClassifyText = "PFClassifier.ClassifyText";
    private const string IpcVersion = "PFClassifier.Version";
    private readonly IIntentClassifier _classifier;

    private readonly ICallGateProvider<string, (Category, float)> _classifyText;
    private readonly ICallGateProvider<Version> _version;

    public IpcProvider(IIntentClassifier classifier)
    {
        _classifier = classifier;

        _classifyText = DalamudService.PluginInterface.GetIpcProvider<string, (Category, float)>(IpcClassifyText);
        _classifyText.RegisterFunc(ClassifyText);

        _version = DalamudService.PluginInterface.GetIpcProvider<Version>(IpcVersion);
        _version.RegisterFunc(() => typeof(PFClassifier).Assembly.GetName().Version!);
    }

    public void Dispose()
    {
        _classifyText.UnregisterFunc();
        _version.UnregisterFunc();
    }

    private (Category, float) ClassifyText(string description)
    {
        return _classifier.ClassifyText(description);
    }
}