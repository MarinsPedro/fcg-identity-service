using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace FiapCloudGames.Identity.Presentation.Pagination;

/// <summary>
/// Codifica posições tipadas como tokens opacos e versionados para transporte HTTP.
/// </summary>
internal static class CursorTokenCodec
{
    private const int CurrentVersion = 1;
    internal const int MaximumTokenLength = 2048;
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Codifica uma posição tipada como Base64 URL-safe.
    /// </summary>
    /// <typeparam name="TCursor">Tipo da posição.</typeparam>
    /// <param name="cursor">Posição a codificar.</param>
    /// <returns>Token opaco e seguro para uso em query string.</returns>
    public static string Encode<TCursor>(TCursor cursor)
        where TCursor : struct
    {
        var envelope = new CursorEnvelope<TCursor>(CurrentVersion, cursor);
        return WebEncoders.Base64UrlEncode(
            JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions));
    }

    /// <summary>
    /// Tenta decodificar um token opaco para sua posição tipada.
    /// </summary>
    /// <typeparam name="TCursor">Tipo da posição esperada.</typeparam>
    /// <param name="token">Token recebido na query string.</param>
    /// <param name="cursor">Posição decodificada.</param>
    /// <returns><see langword="true" /> quando o token é válido e compatível.</returns>
    public static bool TryDecode<TCursor>(string? token, out TCursor cursor)
        where TCursor : struct
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenLength)
        {
            return false;
        }

        try
        {
            var bytes = WebEncoders.Base64UrlDecode(token);
            var envelope = JsonSerializer.Deserialize<CursorEnvelope<TCursor>>(
                bytes,
                SerializerOptions);
            if (envelope is null || envelope.Version != CurrentVersion)
            {
                return false;
            }

            cursor = envelope.Value;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record CursorEnvelope<TCursor>(int Version, TCursor Value)
        where TCursor : struct;
}
