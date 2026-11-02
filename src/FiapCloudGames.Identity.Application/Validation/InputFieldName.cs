using System.Text.Json;

namespace FiapCloudGames.Identity.Application.Validation;

/// <summary>
/// Converte nomes de propriedades .NET em nomes de campos do contrato público.
/// </summary>
public static class InputFieldName
{
    /// <summary>
    /// Converte o primeiro caractere de um nome de propriedade para minúsculo.
    /// </summary>
    /// <param name="propertyName">Nome obtido preferencialmente com <see langword="nameof" />.</param>
    /// <returns>Nome do campo no contrato público.</returns>
    public static string From(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return JsonNamingPolicy.CamelCase.ConvertName(propertyName);
    }
}
