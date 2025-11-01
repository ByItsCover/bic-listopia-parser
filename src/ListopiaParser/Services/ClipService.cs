using System.Net.Http.Json;
using System.Text.Json;
using ListopiaParser.Configs;
using ListopiaParser.ResponseTypes;
using Microsoft.Extensions.Options;

namespace ListopiaParser.Services;

public class ClipService
{
    private readonly HttpClient _client;
    private readonly ClipOptions _options;

    public ClipService(HttpClient client, IOptions<ClipOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<EmbeddingsResponse> GetCoverEmbeddings(List<Edition> editionList, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.ClipURL);
        request.Content = JsonContent.Create( new
        {
            image_urls = editionList.Select(x => x.Image?.Url)
        });
        var response = await _client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var embeddingsJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var embeddings = JsonSerializer.Deserialize<EmbeddingsResponse>(embeddingsJson);

        if (embeddings == null)
        {
            throw new ArgumentNullException(nameof(embeddings), "Embeddings response was unable to be deserialized.");
        }

        embeddings.Editions = editionList;
        return embeddings;
    }
}