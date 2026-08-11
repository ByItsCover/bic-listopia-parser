using GraphQL;
using GraphQL.Client.Http;

namespace Common.Utils;

public static class RequestUtils
{
    public static void HandleGqlErrors<T>(GraphQLResponse<T> response)
    {
        if (response.Errors != null && response.Errors.Any())
        {
            var responseDetails = response.AsGraphQLHttpResponse();
            var exceptions = response.Errors
                .Select(e =>
                    new GraphQLHttpRequestException(responseDetails.StatusCode, responseDetails.ResponseHeaders,
                        e.Message));
            throw new AggregateException(exceptions);
        }
    }
    
    public static async Task<Stream> FetchStream(string url, HttpClient client, CancellationToken cancellationToken)
    {
        var file = await client.GetByteArrayAsync(url, cancellationToken);
        return new MemoryStream(file);
    }
}