using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;

namespace achappey.ChatGPTeams.Repositories;


public interface IEmbeddingRepository
{
    Task<IEnumerable<byte[]>> GetEmbeddingsFromLinesAsync(IEnumerable<string> lines);
    Task<byte[]> GetEmbeddingFromTextAsync(string input);
    List<double> CompareEmbeddings(byte[] query, IEnumerable<byte[]> embeddings);
}

public class EmbeddingRepository : IEmbeddingRepository
{
    private readonly ILogger<EmbeddingRepository> _logger;
    private readonly OpenAIService _openAIService;

    public EmbeddingRepository(ILogger<EmbeddingRepository> logger,
    OpenAIService openAIService)
    {
        _logger = logger;
        _openAIService = openAIService;
    }

    public async Task<byte[]> GetEmbeddingFromTextAsync(string input)
    {
        return await CalculateEmbeddingAsync(input);
    }

    public async Task<IEnumerable<byte[]>> GetEmbeddingsFromLinesAsync(IEnumerable<string> lines)
    {
        const int maxBatchSize = 2000;
        int batchSize = Math.Min(maxBatchSize, lines.Count());
        int suffix = 1;
        var files = new List<byte[]>();

        while (lines.Any())
        {
            try
            {
                List<string> currentBatch = lines.Take(batchSize).ToList();
                byte[] embeddings = await CalculateEmbeddingAsync(currentBatch);

                files.Add(embeddings);
                lines = lines.Skip(batchSize).ToList();
                suffix++;
                batchSize = Math.Min(maxBatchSize, lines.Count());
            }
            catch (Exception)
            {
                batchSize = Math.Max(1, batchSize / 2);
                if (batchSize == 1)
                {
                    break;
                }
            }
        }

        return files;
    }

    private async Task<byte[]> CalculateEmbeddingAsync(object input)
    {
        var embeddingRequest = new EmbeddingCreateRequest()
        {
            Input = input is string ? input as string : null,
            InputAsList = input is List<string> ? input as List<string> : null,
            Model = OpenAI.ObjectModels.Models.TextEmbeddingAdaV2
        };

        var embeddingResult = await _openAIService.Embeddings.CreateEmbedding(embeddingRequest);

        if (!embeddingResult.Successful)
        {
            throw new Exception(embeddingResult.Error?.Message);
        }

        string jsonString = JsonSerializer.Serialize(embeddingResult);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        return jsonBytes;
    }

    public List<double> CompareEmbeddings(byte[] query, IEnumerable<byte[]> embeddings)
    {
        var queryEmbedding = ConvertEmbedding(query);
        var queryEmbeddingVector = queryEmbedding?.Data.FirstOrDefault()?.Embedding.ToArray();

        if (queryEmbeddingVector == null)
        {
            throw new ArgumentException("Invalid query embedding.");
        }

        var mergeEmbeddings = embeddings
            .SelectMany(e =>
            {
                var converted = ConvertEmbedding(e);
                return converted.Data.Select(d => CalculateCosineSimilarity(d.Embedding.ToArray(), queryEmbeddingVector));
            }).ToList();

        return mergeEmbeddings;
    }

    private OpenAI.ObjectModels.ResponseModels.EmbeddingCreateResponse ConvertEmbedding(byte[] embedding)
    {
        var responseJson = Encoding.UTF8.GetString(embedding);

        return JsonSerializer.Deserialize<OpenAI.ObjectModels.ResponseModels.EmbeddingCreateResponse>(responseJson);
    }

    private static double CalculateCosineSimilarity(double[] vector1, double[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            throw new ArgumentException("Vectors must have the same length");
        }

        double dotProduct = 0;
        double norm1 = 0;
        double norm2 = 0;

        int i = 0;
        int length = Vector<double>.Count;

        // Compute dot product, norm1, and norm2 using SIMD instructions
        for (; i <= vector1.Length - length; i += length)
        {
            var vec1 = new Vector<double>(vector1, i);
            var vec2 = new Vector<double>(vector2, i);
            dotProduct += Vector.Dot(vec1, vec2);
            norm1 += Vector.Dot(vec1, vec1);
            norm2 += Vector.Dot(vec2, vec2);
        }

        // Compute the remaining elements using scalar operations
        for (; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            norm1 += vector1[i] * vector1[i];
            norm2 += vector2[i] * vector2[i];
        }

        if (norm1 == 0 || norm2 == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

}
