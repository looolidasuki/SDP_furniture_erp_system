$ErrorActionPreference = 'Stop'
Set-Location 'C:\Users\user\source\repos\4915M_claude'

git fetch origin v3
git checkout -B v3 origin/v3

$csproj = 'FurnitureERP\FurnitureERP.csproj'
$content = Get-Content $csproj -Raw
$needle = '<Compile Include="Helpers\PdfExportHelper.cs" />'
$insert = @"
$needle
    <Compile Include="Helpers\ReplySlipPdfHelper.cs" />
"@

if ($content -notmatch 'ReplySlipPdfHelper\.cs') {
    $content = $content.Replace($needle, $insert.TrimEnd())
    Set-Content -Path $csproj -Value $content -NoNewline
}

git add $csproj
git diff --cached --stat
git commit -m "Include ReplySlipPdfHelper in project compile list."
git push origin v3
git checkout v2

Write-Host 'Done. v3 updated and switched back to v2.'
