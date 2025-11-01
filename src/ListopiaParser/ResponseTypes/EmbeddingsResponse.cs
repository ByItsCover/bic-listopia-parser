using System.Text.Json.Serialization;

namespace ListopiaParser.ResponseTypes;

public class EmbeddingsResponse
{
    [JsonPropertyName("image_embeddings")]
    public required List<ReadOnlyMemory<float>?> ImageEmbeddings  { get; init; }
    [JsonIgnore]
    public List<Edition>? Editions { get; set; }
}