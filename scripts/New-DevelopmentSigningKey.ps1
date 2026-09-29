[CmdletBinding()]
param(
    [Parameter()]
    [string] $OutputPath = (Join-Path $PSScriptRoot "..\secrets\identity-signing-key.pem")
)

$resolvedDirectory = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($OutputPath))
[System.IO.Directory]::CreateDirectory($resolvedDirectory) | Out-Null

if (Test-Path -LiteralPath $OutputPath) {
    throw "O arquivo já existe: $OutputPath"
}

$rsa = [System.Security.Cryptography.RSA]::Create(3072)

try {
    [System.IO.File]::WriteAllText(
        [System.IO.Path]::GetFullPath($OutputPath),
        $rsa.ExportPkcs8PrivateKeyPem(),
        [System.Text.UTF8Encoding]::new($false))
}
finally {
    $rsa.Dispose()
}

Write-Host "Chave RSA de desenvolvimento criada em $([System.IO.Path]::GetFullPath($OutputPath))."
Write-Host "Não versione esse arquivo. Configure Jwt__PrivateKeyPath apontando para ele."
