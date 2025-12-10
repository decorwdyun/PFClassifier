using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using ECommons.SimpleGui;
using PFClassifier.IPC;
using PFClassifier.Services;

namespace PFClassifier;

// ReSharper disable once InconsistentNaming
public class PFClassifier : IDalamudPlugin
{
    private readonly IntentClassifier _intentClassifier;

    public PFClassifier(IDalamudPluginInterface pi, IClientState clientState, IToastGui toastGui)
    {
#if RELEASE
        if ((uint)clientState.ClientLanguage != 4)
        {
            throw new InvalidOperationException("This plugin is not compatible with your client.");
        }
#endif
    
        ECommonsMain.Init(pi, this);
        
        _intentClassifier = new IntentClassifier();
        _intentClassifier.LoadModelFromEmbeddedResource();

        _ = new IpcProvider(_intentClassifier);
    }

    public void Dispose()
    {
        _intentClassifier.Dispose();
        ECommonsMain.Dispose();
    }
}