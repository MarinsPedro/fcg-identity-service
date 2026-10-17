using FiapCloudGames.BuildingBlocks.Exceptions;
using FiapCloudGames.Identity.Application.Validation;

namespace FiapCloudGames.Identity.Application.Queries;

/// <summary>
/// Centraliza validações comuns de entradas de consulta da Application.
/// </summary>
public static class QueryValidation
{
    /// <summary>
    /// Normaliza um filtro textual opcional e valida seu tamanho máximo.
    /// </summary>
    /// <param name="value">Valor recebido pela consulta.</param>
    /// <param name="maximumLength">Quantidade máxima de caracteres.</param>
    /// <param name="field">Nome público do campo usado na mensagem de validação.</param>
    /// <returns>Valor sem espaços externos ou <see langword="null" />.</returns>
    public static string? NormalizeOptional(
        string? value,
        int maximumLength,
        string field)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumLength);
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw AppException.Validation(
                $"{InputFieldName.From(field)} deve possuir no máximo {maximumLength} caracteres.");
    }

    /// <summary>
    /// Garante que uma data opcional esteja em UTC.
    /// </summary>
    /// <param name="value">Data recebida pela consulta.</param>
    /// <param name="field">Nome público do campo usado na mensagem de validação.</param>
    public static void EnsureUtc(DateTimeOffset? value, string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(field);

        if (value is not null && value.Value.Offset != TimeSpan.Zero)
        {
            throw AppException.Validation(
                $"{InputFieldName.From(field)} deve estar em UTC.");
        }
    }

    /// <summary>
    /// Garante que o início opcional de um período não seja posterior ao fim.
    /// </summary>
    /// <param name="fromUtc">Início inclusivo do período.</param>
    /// <param name="toUtc">Fim inclusivo do período.</param>
    /// <param name="fromField">Nome público do campo inicial.</param>
    /// <param name="toField">Nome público do campo final.</param>
    public static void EnsureValidPeriod(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string fromField,
        string toField)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromField);
        ArgumentException.ThrowIfNullOrWhiteSpace(toField);

        if (fromUtc > toUtc)
        {
            throw AppException.Validation(
                $"{InputFieldName.From(fromField)} não pode ser posterior a " +
                $"{InputFieldName.From(toField)}.");
        }
    }
}
