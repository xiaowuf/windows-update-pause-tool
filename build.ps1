$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDir = Join-Path $projectRoot 'dist'
$compilerCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$compiler = $compilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $compiler) {
    throw 'The .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$outputFile = Join-Path $outputDir 'WindowsUpdatePauseTool.exe'
$sourceFile = Join-Path $projectRoot 'src\Program.cs'
$manifestFile = Join-Path $projectRoot 'app.manifest'
$iconFile = Join-Path $projectRoot 'assets\app.ico'

& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /win32manifest:$manifestFile /win32icon:$iconFile /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:$outputFile $sourceFile
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "Build completed: $outputFile"
