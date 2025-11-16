using ECommons.EzIpcManager;
using PFClassifier.Models;
using PFClassifier.Services;

namespace PFClassifier.IPC;

public class IpcProvider : IDisposable
{
    private readonly IntentClassifier _classifier;

    public IpcProvider(IntentClassifier classifier)
    {
        _classifier = classifier;
        EzIPC.Init(this);
    }

    [EzIPC]
    public (Category, float) ClassifyText(string description)
    {
        return _classifier.ClassifyText(description);
    }
    
    public void Dispose()
    {
    }
}