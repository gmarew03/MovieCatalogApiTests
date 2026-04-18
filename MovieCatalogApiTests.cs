using System.Net;
using System.Text.Json;
using MovieCatalogApiTests.Dtos;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;

namespace MovieCatalogApiTests;

[TestFixture]
public class MovieCatalogApiTests
{
    private RestClient? client;
    private static string? accessToken;
    private static string? createdMovieId;

    private const string BaseUrl = "http://144.91.123.158:5000/api";

    private const string Email = "gogomarev1@gmail.com";
    private const string Password = "1122334455";

    [OneTimeSetUp]
    public void Setup()
    {
        client = new RestClient(BaseUrl);

        var request = new RestRequest("User/Authentication", Method.Post);
        request.AddJsonBody(new
        {
            email = Email,
            password = Password
        });

        var response = client.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);
        accessToken = GetStringProperty(doc.RootElement, "accessToken");

        Assert.That(accessToken, Is.Not.Null.And.Not.Empty);

        client = new RestClient(new RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(accessToken!)
        });
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        client?.Dispose();
    }

    [Test, Order(1)]
    public void CreateMovie_WithRequiredFields_ShouldSucceed()
    {
        var request = new RestRequest("Movie/Create", Method.Post);
        request.AddJsonBody(new
        {
            title = $"Test Movie {Guid.NewGuid()}",
            description = "Test Description",
            posterUrl = "https://upload.wikimedia.org/wikipedia/commons/3/3f/Fronalpstock_big.jpg",
            trailerLink = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            isWatched = false
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);

        var message = ExtractMessage(doc.RootElement);
        var movieId = ExtractMovieId(doc.RootElement);

        Assert.That(movieId, Is.Not.Null.And.Not.Empty);
        Assert.That(message, Is.EqualTo("Movie created successfully!"));

        createdMovieId = movieId;
    }

    [Test, Order(2)]
    public void EditCreatedMovie_ShouldSucceed()
    {
        var request = new RestRequest("Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", createdMovieId!);
        request.AddJsonBody(new
        {
            title = "Edited Test Movie",
            description = "Edited Test Description",
            posterUrl = "https://upload.wikimedia.org/wikipedia/commons/3/3f/Fronalpstock_big.jpg",
            trailerLink = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            isWatched = true
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);
        var message = ExtractMessage(doc.RootElement);

        Assert.That(message, Is.EqualTo("Movie edited successfully!"));
    }

    [Test, Order(3)]
    public void GetAllMovies_ShouldReturnNonEmptyArray()
    {
        var request = new RestRequest("Catalog/All", Method.Get);

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);
        var count = ExtractMovieArrayCount(doc.RootElement);

        Assert.That(count, Is.GreaterThan(0));
    }

    [Test, Order(4)]
    public void DeleteCreatedMovie_ShouldSucceed()
    {
        var request = new RestRequest("Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", createdMovieId!);

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);
        var message = ExtractMessage(doc.RootElement);

        Assert.That(message, Is.EqualTo("Movie deleted successfully!"));
    }

    [Test, Order(5)]
    public void CreateMovie_WithoutRequiredFields_ShouldFail()
    {
        var request = new RestRequest("Movie/Create", Method.Post);
        request.AddJsonBody(new
        {
            title = "",
            description = ""
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order(6)]
    public void EditNonExistingMovie_ShouldFail()
    {
        var request = new RestRequest("Movie/Edit", Method.Put);
        request.AddQueryParameter("movieId", "12345678-1234-1234-1234-123456789012");
        request.AddJsonBody(new
        {
            title = "Fake",
            description = "Fake",
            posterUrl = "",
            trailerLink = "",
            isWatched = false
        });

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);
        var message = ExtractMessage(doc.RootElement);

        Assert.That(message, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
    }

    [Test, Order(7)]
    public void DeleteNonExistingMovie_ShouldFail()
    {
        var request = new RestRequest("Movie/Delete", Method.Delete);
        request.AddQueryParameter("movieId", "12345678-1234-1234-1234-123456789012");

        var response = client!.Execute(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content, Is.Not.Null.And.Not.Empty);

        using var doc = JsonDocument.Parse(response.Content!);
        var message = ExtractMessage(doc.RootElement);

        Assert.That(message, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
    }

    private static string? ExtractMessage(JsonElement root)
    {
        if (TryGetPropertyIgnoreCase(root, "msg", out var msgElement) && msgElement.ValueKind == JsonValueKind.String)
            return msgElement.GetString();

        if (TryGetPropertyIgnoreCase(root, "message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
            return messageElement.GetString();

        if (TryGetPropertyIgnoreCase(root, "title", out var titleElement) && titleElement.ValueKind == JsonValueKind.String)
            return titleElement.GetString();

        return null;
    }

    private static string? ExtractMovieId(JsonElement root)
    {
        if (TryGetPropertyIgnoreCase(root, "movie", out var movieElement) && movieElement.ValueKind == JsonValueKind.Object)
        {
            if (TryGetPropertyIgnoreCase(movieElement, "id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                return idElement.GetString();
        }

        if (TryGetPropertyIgnoreCase(root, "id", out var rootIdElement) && rootIdElement.ValueKind == JsonValueKind.String)
            return rootIdElement.GetString();

        return null;
    }

    private static int ExtractMovieArrayCount(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root.GetArrayLength();

        string[] names = ["movies", "data", "items", "results", "catalog"];

        foreach (var name in names)
        {
            if (TryGetPropertyIgnoreCase(root, name, out var arr) && arr.ValueKind == JsonValueKind.Array)
                return arr.GetArrayLength();
        }

        return 0;
    }

    private static string? GetStringProperty(JsonElement root, string name)
    {
        return TryGetPropertyIgnoreCase(root, name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}