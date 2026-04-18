namespace MovieCatalogApiTests.Dtos;

public class MovieDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? TrailerLink { get; set; }
    public bool? IsWatched { get; set; }
}