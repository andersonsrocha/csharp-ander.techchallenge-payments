using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace TechChallengePayments.Elasticsearch;

public class ElasticClient<T>(IElasticSettings settings) : IElasticClient<T>
{
    private readonly ElasticsearchClient _client = new(settings.CloudId, new ApiKey(settings.ApiKey));

    public async Task<IReadOnlyCollection<T>> Get(int page, int size, IndexName index)
    {
        var response = await _client.SearchAsync<T>(s => s.Indices(index).From(page).Size(size));
        return response.Documents;
    }

    public async Task<bool> AddOrUpdate(T item, IndexName index)
    {
        var response = await _client.IndexAsync(item, i => i.Index(index));
        return response.IsValidResponse;
    }
}