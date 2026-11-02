using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Identity.Application.Validation;

namespace FiapCloudGames.Identity.Presentation.Validation;

/// <summary>
/// Valida tamanho textual usando automaticamente o nome público da propriedade.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class ApiStringLengthAttribute(int maximumLength)
    : StringLengthAttribute(maximumLength)
{
    /// <inheritdoc />
    public override string FormatErrorMessage(string name)
    {
        var field = InputFieldName.From(name);
        return MinimumLength > 0
            ? $"{field} deve possuir entre {MinimumLength} e {MaximumLength} caracteres."
            : $"{field} deve possuir no máximo {MaximumLength} caracteres.";
    }
}
