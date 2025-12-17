using PFClassifier.Models;

namespace PFClassifier.Classifier;

public interface IIntentClassifier : IDisposable
{
    (Category, float) ClassifyText(string description);
    
    void LoadModelFromEmbeddedResource();
}