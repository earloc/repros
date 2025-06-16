using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;
using Microsoft.VisualBasic;
using Repros;

namespace Tests;

public class UnitTest1
{
    [Fact]
    public async Task CanNotHandleNumbersAsStrings()
    {
        var builder = new WebApplicationFactory<Program>();

        var client = builder.CreateClient();
        
        var apiClient = new ApiClient(new HttpClientRequestAdapter
        (
            authenticationProvider: new AnonymousAuthenticationProvider(),
            parseNodeFactory: new JsonParseNodeFactory(),
            serializationWriterFactory: new JsonSerializationWriterFactory(),
            httpClient: client
        ));

        // working sample, when the API honors it's openapi-definition
        var responseNumbersAsNumbers = await apiClient[false].GetAsync();
        Assert.NotNull(responseNumbersAsNumbers);
        Assert.Equal(42, responseNumbersAsNumbers.Integer);
        Assert.Equal(13.37, responseNumbersAsNumbers.Double);

        // "broken" sample, when the API *does not* honor it's openapi-definition
        var responseNumberAsString = await apiClient[true].GetAsync();
        Assert.NotNull(responseNumberAsString);
        Assert.Equal(42, responseNumberAsString.Integer); //is null at runtime
        Assert.Equal(13.37, responseNumberAsString.Double); //is null at runtime
    }

    // repro for https://github.com/microsoft/kiota/issues/6667
    [Fact]
    public async Task CanHandleNumbersAsStrings()
    {
        var builder = new WebApplicationFactory<Program>();

        var client = builder.CreateClient();
        var context = new KiotaJsonSerializationContext(new JsonSerializerOptions(JsonSerializerOptions.Web)); // can handle numbers-as-strings per default

        var apiClient = new ApiClient(new HttpClientRequestAdapter
        (
            authenticationProvider: new AnonymousAuthenticationProvider(),
            parseNodeFactory: new SerializationContextAwareJsonParseNodeFactory(context),
            serializationWriterFactory: new JsonSerializationWriterFactory(),
            httpClient: client
        ));

        // working sample, when the API honors it's openapi-definition
        var responseNumbersAsNumbers = await apiClient[false].GetAsync();
        Assert.NotNull(responseNumbersAsNumbers);
        Assert.Equal(42, responseNumbersAsNumbers.Integer);
        Assert.Equal(13.37, responseNumbersAsNumbers.Double);

        // "broken" sample, when the API *does not* honor it's openapi-definition
        var responseNumberAsString = await apiClient[true].GetAsync();
        Assert.NotNull(responseNumberAsString);
        Assert.Equal(42, responseNumberAsString.Integer); //is null at runtime
        Assert.Equal(13.37, responseNumberAsString.Double); //is null at runtime
    }
}
