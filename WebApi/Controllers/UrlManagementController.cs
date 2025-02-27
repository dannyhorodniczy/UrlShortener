using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class UrlManagementController : ControllerBase
{
    [HttpGet]
    public string GetAndDoSomething()
    {
        return $"Is this running on Linux? {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}";
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> RedirectAsync(NpgsqlDataSource dataSource, Guid id)
    {
        // TODO: ensure no duplicates at the database level
        await using var command = dataSource.CreateCommand(
            $"""
            SELECT * FROM url.urls
            WHERE id = '{id}'
            LIMIT 1
            """);
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            int ordinal = reader.GetOrdinal("longUrl");
            var longUrl = reader.GetString(ordinal);
            return Redirect(longUrl);
        }

        return NotFound();
    }

    [HttpPost]
    public async Task<string> CreateShortUrlAsync(NpgsqlDataSource dataSource, [FromBody] EncodeUrlRequest request)
    {
        // TODO: ensure no sql injection
        await using var command = dataSource.CreateCommand($"""
    SELECT id
    FROM url.urls
    WHERE longUrl = '{request.url}'
    LIMIT 1
    """);
        var test = await command.ExecuteScalarAsync();
        if (test is Guid guid)
        {
            return BuildUrl(Request, guid);
        }

        return await EncodeAndSaveUrlAsync(dataSource, request.url);
    }

    private async Task<string> EncodeAndSaveUrlAsync(NpgsqlDataSource dataSource, string longUrl)
    {
        var id = Guid.NewGuid();

        await using var command = dataSource.CreateCommand($"""
    INSERT INTO url.urls
    VALUES ('{id}', '{longUrl}'); 
    """);
        await command.ExecuteScalarAsync();

        return BuildUrl(Request, id);
    }

    private static string BuildUrl(HttpRequest request, Guid id)
    {
        return $"{request.Scheme}://{request.Host}/UrlManagement/{id}";
    }
}

public record EncodeUrlRequest(string url);