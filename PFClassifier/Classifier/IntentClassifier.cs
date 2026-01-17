using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.ML;
using Microsoft.ML.Data;
using PFClassifier.Models;

namespace PFClassifier.Classifier;

public partial class IntentClassifier : IIntentClassifier
{
    private PredictionEngine<IntentInput, IntentPrediction>? _predictionEngine;

    public void LoadModelFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("PFClassifier.Models.intent_model.zip");
        if (stream == null)
        {
            DalamudService.Log.Error("Failed to load embedded model resource.");
            return;
        }

        var mlContext = new MLContext();
        var model = mlContext.Model.Load(stream, out _);
        _predictionEngine = mlContext.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(model);
    }

    public (Category, float) ClassifyText(string description)
    {
        var sw = Stopwatch.StartNew();
        if (_predictionEngine == null) throw new InvalidOperationException("Model not loaded.");

        float maxScore = 0;

        var input = new IntentInput
        {
            Text = NormalizeText(description)
        };

        var prediction = _predictionEngine.Predict(input);
        var predictedLabel = prediction.PredictedLabel;
        maxScore = prediction.Score.Prepend(maxScore).Max();
        
        sw.Stop();
        if (Enum.TryParse(predictedLabel, out Category category))
        {
            DalamudService.Log.Verbose(
                $"模型预测：{category} 置信度：{maxScore:P2}，耗时：{sw.Elapsed.TotalMilliseconds:F2}ms，文本：{description}");
            return (category, maxScore);
        }

        return (Category.未知, 0);
    }

    public void Dispose()
    {
        _predictionEngine?.Dispose();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleSpacesRegex();

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            if (c is '\r' or '\n' or ';' or '；' or '"' or '“' or '”' or ',' or '，')
            {
                sb.Append(' ');
                continue;
            }

            if (c == 0x3000)
            {
                sb.Append(' ');
            }
            else if (c >= 0xFF01 && c <= 0xFF5E)
            {
                sb.Append((char)(c - 0xFEE0));
            }
            else switch (c)
            {
                case >= '①' and <= '⑳':
                {
                    var num = c - '①' + 1;
                    sb.Append(num);
                    break;
                }
                case '⓪':
                    sb.Append('0');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        var result = sb.ToString();

        result = result.ToLowerInvariant().Trim();

        result = MultipleSpacesRegex().Replace(result, " ");

        return result;
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