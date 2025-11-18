using System.Diagnostics;
using System.Reflection;
using System.Text;
using ECommons.DalamudServices;
using Microsoft.ML;
using Microsoft.ML.Data;
using PFClassifier.Models;

namespace PFClassifier.Services;

public partial class IntentClassifier : IDisposable
{
    private PredictionEngine<IntentInput, IntentPrediction>? _predictionEngine;

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
    private static partial System.Text.RegularExpressions.Regex TwoWhiteSpace();

    public void LoadModelFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("PFClassifier.Models.intent_model.zip");
        if (stream == null)
        {
            Svc.Log.Error("Failed to load embedded model resource.");
            return;
        }

        var mlContext = new MLContext();
        var model = mlContext.Model.Load(stream, out var modelSchema);
        _predictionEngine = mlContext.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(model);
    }

    public (Category, float) ClassifyText(string description)
    {
        if (_predictionEngine == null)
        {
            throw new InvalidOperationException("Model not loaded.");
        }

        float maxScore = 0;

        var input = new IntentInput
        {
            Text = NormalizeText(description)
        };

        var prediction = _predictionEngine.Predict(input);
        var predictedLabel = prediction.PredictedLabel;
        // predictedLabel = CleanLabel(prediction.PredictedLabel);
        maxScore = prediction.Score.Prepend(maxScore).Max();
        if (Enum.TryParse(predictedLabel, out Category category))
        {
            Svc.Log.Debug($"模型预测：{category} 置信度：{maxScore:P2}，文本：{description}");
            return (category, maxScore);
        }
        return (Category.未知, 0);
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var c in text)
        {
            if (c is '\r' or '\n' or ';' or '；' or '"' or '“' or '”')
            {
                sb.Append(' ');
                continue;
            }

            if (c >= 0xFF01 && c <= 0xFF5E)
            {
                sb.Append((char)(c - 0xFEE0));
            }
            else if (c == 0x3000)
            {
                sb.Append(' ');
            }
            else
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();
        result = result.ToLowerInvariant().Trim();
        result = TwoWhiteSpace().Replace(result, " ");
        return result;
    }

    public void Dispose()
    {
        _predictionEngine?.Dispose();
    }

    private class IntentInput
    {
        [LoadColumn(0)] public string Text { get; set; } = string.Empty;

        [LoadColumn(1)] public string Label { get; set; } = string.Empty;
    }

    private class IntentPrediction
    {
        [ColumnName("PredictedLabel")] public string PredictedLabel { get; set; } = string.Empty;

        public float[] Score { get; set; } = [];
    }
}