using PFClassifier.Models;

namespace PFClassifier.Classifier;

public class CachedIntentClassifier(IIntentClassifier innerClassifier) : IIntentClassifier
{
    private readonly Lock _lock = new();
    
    private const int MaxCapacity = 1000;
    private const int GenerationCapacity = MaxCapacity / 2;

    private Dictionary<string, (Category, float)> _currentGen = new(GenerationCapacity);
    private Dictionary<string, (Category, float)> _oldGen = new(GenerationCapacity);

    public (Category, float) ClassifyText(string description)
    {
        if (TryGetFromCache(description, out var cachedResult))
        {
            DalamudService.Log.Logger.Verbose("[缓存命中] 模型预测：{Category} 置信度：{Score:P2}，文本：{Description}", cachedResult.Item1, cachedResult.Item2, description);
            return cachedResult;
        }

        var result = innerClassifier.ClassifyText(description);

        AddToCache(description, result);

        return result;
    }

    private bool TryGetFromCache(string key, out (Category, float) value)
    {
        lock (_lock)
        {
            if (_currentGen.TryGetValue(key, out value))
            {
                return true;
            }

            if (_oldGen.TryGetValue(key, out value))
            {
                if (_currentGen.Count < GenerationCapacity)
                {
                    _currentGen[key] = value;
                }
                return true;
            }
        }
        value = default;
        return false;
    }

    private void AddToCache(string key, (Category, float) value)
    {
        lock (_lock)
        {
            if (_currentGen.ContainsKey(key)) return;

            if (_currentGen.Count >= GenerationCapacity)
            {
                _oldGen = _currentGen;
                _currentGen = new Dictionary<string, (Category, float)>(GenerationCapacity);
            }

            _currentGen[key] = value;
        }
    }

    public void LoadModelFromEmbeddedResource()
    {
        innerClassifier.LoadModelFromEmbeddedResource();
    }

    public void Dispose()
    {
        innerClassifier.Dispose();
        lock (_lock)
        {
            _currentGen.Clear();
            _oldGen.Clear();
        }
    }
}