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
        
        bool RepoCheck()
        {
            var sourceRepository = pi.SourceRepository;
            return sourceRepository == "https://gp.xuolu.com/love.json" || sourceRepository.Contains("decorwdyun/DalamudPlugins", StringComparison.OrdinalIgnoreCase);
        }

        if (pi.IsDev || !RepoCheck())
        {
            toastGui.ShowError("此插件禁止本地加载，\n" +
                               "此插件禁止本地加载，\n" +
                               "此插件禁止本地加载，\n");
            throw new InvalidOperationException("此插件禁止本地加载。");
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