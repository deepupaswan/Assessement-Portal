using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CandidateService.Application.Services;
using CandidateService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CandidateService.Infrastructure.Services;

public class CandidateSearchService : ICandidateSearchService
{
    private readonly HttpClient _httpClient;
    private readonly string _indexName;
    private readonly ILogger<CandidateSearchService> _logger;
    private readonly SemaphoreSlim _indexInitLock = new(1, 1);
    private volatile bool _isIndexInitialized;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CandidateSearchService(HttpClient httpClient, IConfiguration configuration, ILogger<CandidateSearchService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var configuredIndexName = configuration["Elasticsearch:IndexName"];
        _indexName = string.IsNullOrWhiteSpace(configuredIndexName)
            ? "candidate-service-candidates"
            : configuredIndexName.Trim().ToLowerInvariant();
    }

    public async Task IndexCandidateAsync(Candidate candidate)
    {
        try
        {
            await EnsureIndexInitializedAsync();
            using var response = await _httpClient.PutAsJsonAsync($"{_indexName}/_doc/{candidate.Id:D}", candidate);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to index candidate {CandidateId} in Elasticsearch", candidate.Id);
        }
    }

    public async Task DeleteCandidateAsync(Guid candidateId)
    {
        try
        {
            await EnsureIndexInitializedAsync();
            using var response = await _httpClient.DeleteAsync($"{_indexName}/_doc/{candidateId:D}");

            if (response.StatusCode is HttpStatusCode.NotFound)
                return;

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete candidate {CandidateId} from Elasticsearch", candidateId);
        }
    }

    public async Task<CandidateSearchResult> SearchCandidatesAsync(
        string query,
        int page = 1,
        int size = 20,
        string? email = null,
        DateTime? createdFromUtc = null,
        DateTime? createdToUtc = null,
        string? sortBy = null,
        string? sortOrder = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new CandidateSearchResult(0, Math.Max(page, 1), Math.Clamp(size, 1, 100), Array.Empty<Candidate>());

        await EnsureIndexInitializedAsync();

        var safePage = Math.Max(page, 1);
        var safeSize = Math.Clamp(size, 1, 100);
        var from = (safePage - 1) * safeSize;
        var normalizedSortOrder = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        var normalizedSortBy = (sortBy ?? "score").Trim().ToLowerInvariant();

        var must = new List<object>
        {
            new
            {
                multi_match = new
                {
                    query,
                    fields = new[] { "name^2", "email" },
                    fuzziness = "AUTO"
                }
            }
        };

        var filters = new List<object>();

        if (!string.IsNullOrWhiteSpace(email))
        {
            filters.Add(new
            {
                term = new Dictionary<string, object?>
                {
                    ["email.keyword"] = email.Trim().ToLowerInvariant()
                }
            });
        }

        if (createdFromUtc.HasValue || createdToUtc.HasValue)
        {
            var range = new Dictionary<string, object?>();
            if (createdFromUtc.HasValue)
                range["gte"] = createdFromUtc.Value;
            if (createdToUtc.HasValue)
                range["lte"] = createdToUtc.Value;

            filters.Add(new
            {
                range = new Dictionary<string, object?>
                {
                    ["createdAt"] = range
                }
            });
        }

        object[] sort = normalizedSortBy switch
        {
            "name" =>
            [
                new Dictionary<string, object?> { ["name.keyword"] = new { order = normalizedSortOrder } },
                new Dictionary<string, object?> { ["createdAt"] = new { order = "desc" } }
            ],
            "email" =>
            [
                new Dictionary<string, object?> { ["email.keyword"] = new { order = normalizedSortOrder } },
                new Dictionary<string, object?> { ["createdAt"] = new { order = "desc" } }
            ],
            "createdat" =>
            [
                new Dictionary<string, object?> { ["createdAt"] = new { order = normalizedSortOrder } }
            ],
            _ =>
            [
                new Dictionary<string, object?> { ["_score"] = new { order = "desc" } },
                new Dictionary<string, object?> { ["createdAt"] = new { order = "desc" } }
            ]
        };

        var request = new
        {
            from,
            size = safeSize,
            query = new
            {
                @bool = new
                {
                    must,
                    filter = filters
                }
            },
            sort
        };

        using var response = await _httpClient.PostAsJsonAsync($"{_indexName}/_search", request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new CandidateSearchResult(0, safePage, safeSize, Array.Empty<Candidate>());

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ElasticsearchSearchResponse<Candidate>>();
        var items = payload?.Hits?.Documents
            .Where(hit => hit.Source is not null)
            .Select(hit => hit.Source!)
            .ToList()
            ?? new List<Candidate>();

        var total = payload?.Hits?.Total?.Value ?? items.Count;
        return new CandidateSearchResult(total, safePage, safeSize, items);
    }

    public async Task<CandidateReindexResult> ReindexCandidatesAsync(IEnumerable<Candidate> candidates)
    {
        var candidateList = candidates.ToList();
        if (candidateList.Count == 0)
            return new CandidateReindexResult(0, 0, 0);

        await EnsureIndexInitializedAsync();

        var ndjson = new StringBuilder(capacity: candidateList.Count * 160);
        foreach (var candidate in candidateList)
        {
            ndjson.Append("{\"index\":{\"_index\":\"")
                .Append(_indexName)
                .Append("\",\"_id\":\"")
                .Append(candidate.Id.ToString("D"))
                .AppendLine("\"}}");

            ndjson.AppendLine(JsonSerializer.Serialize(candidate, JsonOptions));
        }

        using var content = new StringContent(ndjson.ToString(), Encoding.UTF8, "application/x-ndjson");
        using var response = await _httpClient.PostAsync("_bulk?refresh=wait_for", content);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BulkResponse>();
        var items = payload?.Items ?? new List<BulkItem>();
        var failed = items.Count(item => item.Index.Status >= 300 || item.Index.Error is not null);
        var indexed = candidateList.Count - failed;

        return new CandidateReindexResult(candidateList.Count, indexed, failed);
    }

    public async Task ResetIndexAsync()
    {
        await _indexInitLock.WaitAsync();
        try
        {
            using var deleteResponse = await _httpClient.DeleteAsync(_indexName);
            if (deleteResponse.StatusCode != HttpStatusCode.NotFound)
                deleteResponse.EnsureSuccessStatusCode();

            _isIndexInitialized = false;
            _logger.LogInformation("Elasticsearch index {IndexName} reset requested.", _indexName);
        }
        finally
        {
            _indexInitLock.Release();
        }

        await EnsureIndexInitializedAsync();
    }

    private async Task EnsureIndexInitializedAsync()
    {
        if (_isIndexInitialized)
            return;

        await _indexInitLock.WaitAsync();
        try
        {
            if (_isIndexInitialized)
                return;

            using var existsResponse = await _httpClient.GetAsync(_indexName);
            if (existsResponse.IsSuccessStatusCode)
            {
                _isIndexInitialized = true;
                return;
            }

            if (existsResponse.StatusCode != HttpStatusCode.NotFound)
                existsResponse.EnsureSuccessStatusCode();

            var requestPayload = new Dictionary<string, object?>
            {
                ["settings"] = new
                {
                    analysis = new
                    {
                        normalizer = new
                        {
                            lowercase_normalizer = new
                            {
                                type = "custom",
                                filter = new[] { "lowercase" }
                            }
                        }
                    }
                },
                ["mappings"] = new Dictionary<string, object?>
                {
                    ["dynamic"] = "strict",
                    ["properties"] = new
                    {
                        id = new { type = "keyword" },
                        name = new
                        {
                            type = "text",
                            fields = new
                            {
                                keyword = new { type = "keyword", ignore_above = 256 }
                            }
                        },
                        email = new
                        {
                            type = "text",
                            fields = new
                            {
                                keyword = new { type = "keyword", normalizer = "lowercase_normalizer" }
                            }
                        },
                        createdAt = new { type = "date" },
                        updatedAt = new { type = "date" }
                    }
                }
            };

            using var createResponse = await _httpClient.PutAsJsonAsync(_indexName, requestPayload);

            if (createResponse.StatusCode != HttpStatusCode.Conflict)
                createResponse.EnsureSuccessStatusCode();

            _isIndexInitialized = true;
            _logger.LogInformation("Elasticsearch index {IndexName} is ready.", _indexName);
        }
        finally
        {
            _indexInitLock.Release();
        }
    }

    private sealed class ElasticsearchSearchResponse<T>
    {
        [JsonPropertyName("hits")]
        public ElasticsearchHits<T> Hits { get; set; } = new();
    }

    private sealed class ElasticsearchHits<T>
    {
        [JsonPropertyName("total")]
        public ElasticsearchTotal? Total { get; set; }

        [JsonPropertyName("hits")]
        public List<ElasticsearchHit<T>> Documents { get; set; } = new();
    }

    private sealed class ElasticsearchTotal
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("relation")]
        public string Relation { get; set; } = string.Empty;
    }

    private sealed class ElasticsearchHit<T>
    {
        [JsonPropertyName("_source")]
        public T? Source { get; set; }
    }

    private sealed class BulkResponse
    {
        [JsonPropertyName("items")]
        public List<BulkItem> Items { get; set; } = new();
    }

    private sealed class BulkItem
    {
        [JsonPropertyName("index")]
        public BulkIndexResult Index { get; set; } = new();
    }

    private sealed class BulkIndexResult
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("error")]
        public object? Error { get; set; }
    }
}