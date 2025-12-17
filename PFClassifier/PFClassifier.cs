using Dalamud.Plugin;
using PFClassifier.Classifier;
using PFClassifier.IPC;
using IntentClassifier = PFClassifier.Classifier.IntentClassifier;

namespace PFClassifier;

// ReSharper disable once InconsistentNaming
public class PFClassifier : IDalamudPlugin
{
    private readonly IIntentClassifier _intentClassifier;
    private readonly IpcProvider _ipcProvider;

    public PFClassifier(IDalamudPluginInterface pi)
    {
        pi.Create<DalamudService>();
#if RELEASE
        if ((uint)DalamudService.ClientState.ClientLanguage != 4)
        {
            throw new InvalidOperationException("This plugin is not compatible with your client.");
        }
#endif

        _intentClassifier = new CachedIntentClassifier(new IntentClassifier());
        _intentClassifier.LoadModelFromEmbeddedResource();
        _ipcProvider = new IpcProvider(_intentClassifier);
    }

    public void Dispose()
    {
        _ipcProvider.Dispose();
        _intentClassifier.Dispose();
        GC.SuppressFinalize(this);
    }
}