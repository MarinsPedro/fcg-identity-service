using System.ComponentModel.DataAnnotations;
using FiapCloudGames.Identity.Application.Validation;

namespace FiapCloudGames.Identity.Presentation.Validation;

/// <summary>
/// Valida um intervalo inteiro usando automaticamente o nome público da propriedade.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class ApiIntegerRangeAttribute(int minimum, int maximum)
    : RangeAttribute(minimum, maximum)
{
    /// <inheritdoc />
    public override string FormatErrorMessage(string name) =>
        $"{InputFieldName.From(name)} deve estar entre {Minimum} e {Maximum}.";
}
