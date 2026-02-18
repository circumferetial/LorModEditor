param(
    [string]$Configuration = 'Release',
    [string]$ReferenceAssemblyPath = '',
    [string]$SolutionPath = 'Synthesis.slnx',
    [string]$ReportDir = 'docs/quality/reports',
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info([string]$msg) {
    Write-Host "[audit] $msg"
}

function Resolve-RepoRoot {
    $root = (& git rev-parse --show-toplevel 2>$null)
    if (-not $root) {
        throw 'Not inside a git repository.'
    }
    return $root.Trim()
}

function Get-ShortPath([string]$repoRoot, [string]$fullPath) {
    try {
        return [IO.Path]::GetRelativePath($repoRoot, $fullPath)
    }
    catch {
        return $fullPath
    }
}

function Scan-Pattern([string]$repoRoot, [string]$pattern, [string]$label) {
    $lines = @()
    try {
        $output = & rg -n --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/.git/**' -e $pattern Synthesis Synthesis.Plugins.Backup 2>$null
        if ($LASTEXITCODE -eq 0 -and $output) {
            $lines = @($output)
        }
    }
    catch {
        $lines = @()
    }

    return [pscustomobject]@{
        Label = $label
        Pattern = $pattern
        Hits = $lines
        Count = $lines.Count
    }
}

function Get-LargestFiles([string]$repoRoot, [int]$top = 20) {
    $files = Get-ChildItem -Path (Join-Path $repoRoot 'Synthesis') -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }

    $rows = foreach ($f in $files) {
        [pscustomobject]@{
            Path = Get-ShortPath $repoRoot $f.FullName
            Lines = @((Get-Content $f.FullName)).Count
        }
    }

    return $rows | Sort-Object Lines -Descending | Select-Object -First $top
}

function Get-MethodHeavyFiles([string]$repoRoot, [int]$top = 20) {
    $files = Get-ChildItem -Path (Join-Path $repoRoot 'Synthesis') -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }

    $rows = foreach ($f in $files) {
        $raw = [string](Get-Content $f.FullName -Raw -ErrorAction SilentlyContinue)
        $count = ([regex]::Matches($raw, '(?m)^\s*(public|private|protected|internal)\s+[^\n\r;]+\(')).Count
        [pscustomobject]@{
            Path = Get-ShortPath $repoRoot $f.FullName
            Methods = $count
        }
    }

    return $rows | Sort-Object Methods -Descending | Select-Object -First $top
}

function Compare-Assemblies([string]$repoRoot, [string]$referenceAssemblyPath) {
    $referenceAssemblyPath = [string](@($referenceAssemblyPath) | Select-Object -First 1)
    $result = [ordered]@{
        Enabled = $false
        Message = 'Reference assembly path not provided or not found.'
        TypeDiffCount = $null
        MethodDiffCount = $null
        CriticalTypeDiff = @()
    }

    if ([string]::IsNullOrWhiteSpace($referenceAssemblyPath) -or -not (Test-Path $referenceAssemblyPath)) {
        return $result
    }

    $localDll = Join-Path $repoRoot "Synthesis\bin\$Configuration\net10.0-windows\Synthesis.dll"
    if (-not (Test-Path $localDll)) {
        $result.Message = "Local compiled assembly not found: $localDll"
        return $result
    }

    $monoCecilCandidates = @(
        (Join-Path (Split-Path $referenceAssemblyPath -Parent) 'Mono.Cecil.dll')
        (Join-Path $repoRoot 'Synthesis\bin\Debug\net10.0-windows\Mono.Cecil.dll')
        (Join-Path $repoRoot 'Synthesis\bin\Release\net10.0-windows\Mono.Cecil.dll')
    )

    $monoCecilPath = $monoCecilCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $monoCecilPath) {
        $result.Message = 'Mono.Cecil.dll not found; parity diff skipped.'
        return $result
    }

    Add-Type -Path $monoCecilPath

    $result.Enabled = $true

    $refAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($referenceAssemblyPath)
    $locAsm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($localDll)

    $refTypes = $refAsm.MainModule.Types | Where-Object { -not $_.Name.StartsWith('<') } | ForEach-Object FullName | Sort-Object -Unique
    $locTypes = $locAsm.MainModule.Types | Where-Object { -not $_.Name.StartsWith('<') } | ForEach-Object FullName | Sort-Object -Unique

    $typeDiff = Compare-Object -ReferenceObject $locTypes -DifferenceObject $refTypes
    $result.TypeDiffCount = @($typeDiff).Count

    $criticalTypes = @(
        'Synthesis.Core.Abstraction.BaseRepository`1',
        'Synthesis.Core.Abstraction.IGameRepository',
        'Synthesis.Core.ProjectManager',
        'Synthesis.Feature.SkinEditor.SkinRepository',
        'Synthesis.Core.Tools.UnityRichTextHelper'
    )

    $criticalDiff = New-Object System.Collections.Generic.List[string]

    foreach ($ct in $criticalTypes) {
        $rt = $refAsm.MainModule.GetType($ct)
        $lt = $locAsm.MainModule.GetType($ct)

        if (-not $rt -or -not $lt) {
            $criticalDiff.Add("$ct => missing in one side") | Out-Null
            continue
        }

        $rMethods = $rt.Methods |
            Where-Object { -not $_.IsGetter -and -not $_.IsSetter -and -not $_.IsAddOn -and -not $_.IsRemoveOn } |
            ForEach-Object {
                $p = ($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ','
                "$($_.ReturnType.FullName) $($_.Name)($p)"
            } | Sort-Object -Unique

        $lMethods = $lt.Methods |
            Where-Object { -not $_.IsGetter -and -not $_.IsSetter -and -not $_.IsAddOn -and -not $_.IsRemoveOn } |
            ForEach-Object {
                $p = ($_.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ','
                "$($_.ReturnType.FullName) $($_.Name)($p)"
            } | Sort-Object -Unique

        $md = Compare-Object -ReferenceObject $lMethods -DifferenceObject $rMethods
        if (@($md).Count -gt 0) {
            $criticalDiff.Add("$ct => method signature diff count: $(@($md).Count)") | Out-Null
        }
    }

    $result.MethodDiffCount = $criticalDiff.Count
    $result.CriticalTypeDiff = $criticalDiff
    $result.Message = 'Assembly parity comparison executed.'
    return $result
}

$repoRoot = Resolve-RepoRoot
Set-Location $repoRoot

New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$latestReport = Join-Path $ReportDir 'latest-audit.md'
$versionedReport = Join-Path $ReportDir "$timestamp-audit.md"
$baselinePath = Join-Path $ReportDir 'baseline.json'

$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
$head = (& git rev-parse HEAD).Trim()
$headShort = (& git rev-parse --short HEAD).Trim()
$status = (& git status --short)
$testProjects = @(Get-ChildItem -Path $repoRoot -Recurse -File -Filter '*.csproj' |
        Where-Object { $_.FullName -match 'test|tests|spec' } |
        ForEach-Object { Get-ShortPath $repoRoot $_.FullName })

$baselineObj = [ordered]@{
    timestamp = (Get-Date).ToString('o')
    branch = $branch
    head = $head
    headShort = $headShort
    dirty = [bool]($status)
    referenceAssemblyPath = $ReferenceAssemblyPath
}
$baselineObj | ConvertTo-Json -Depth 5 | Set-Content -Encoding utf8 $baselinePath

$buildOutputs = @()
$buildFailed = $false
$warningLines = @()
$errorLines = @()

if (-not $SkipBuild) {
    $projects = @(
        'Synthesis\Synthesis.csproj',
        'Synthesis.Plugins.Backup\Synthesis.Plugins.Backup.csproj'
    )
    if ($testProjects.Count -gt 0) {
        $projects += $testProjects
    }

    foreach ($proj in $projects) {
        Write-Info "Building $proj ($Configuration)"
        $output = & dotnet build $proj -c $Configuration -nologo -p:UseAppHost=false 2>&1
        $buildOutputs += "### Build: $proj"
        $buildOutputs += $output

        if ($LASTEXITCODE -ne 0) {
            $buildFailed = $true
        }

        $warningLines += $output | Where-Object { $_ -match ': warning ' }
        $errorLines += $output | Where-Object { $_ -match ': error ' }
    }
}

$patterns = @(
    (Scan-Pattern $repoRoot '\basync\s+void\b' 'async void'),
    (Scan-Pattern $repoRoot 'catch\s*\{\s*\}' 'empty catch block'),
    (Scan-Pattern $repoRoot 'catch\s*\{' 'catch without exception variable'),
    (Scan-Pattern $repoRoot 'Task\.Run\(' 'Task.Run usage'),
    (Scan-Pattern $repoRoot 'MessageBox\.Show\(' 'MessageBox usage'),
    (Scan-Pattern $repoRoot 'lock\s*\(' 'lock statement'),
    (Scan-Pattern $repoRoot 'namespace\s+Synthesis\.Core\.Abstraction' 'Core Abstraction namespace (for layering checks)')
)

$largestFiles = Get-LargestFiles $repoRoot 20
$methodHeavy = Get-MethodHeavyFiles $repoRoot 20

$hasEditorConfig = Test-Path (Join-Path $repoRoot '.editorconfig')
$hasDirectoryBuildProps = Test-Path (Join-Path $repoRoot 'Directory.Build.props')
$parity = Compare-Assemblies $repoRoot $ReferenceAssemblyPath

# Findings (P0..P3)
$findings = New-Object System.Collections.Generic.List[psobject]

if ($buildFailed) {
    $findings.Add([pscustomobject]@{
        Severity = 'P0'
        Location = 'Build Gate B'
        Issue = 'Release build failed'
        Impact = 'Cannot trust audit results until build baseline is green'
        Fix = 'Resolve compile errors, then rerun audit and parity checks'
    }) | Out-Null
}

 $emptyCatchPattern = @($patterns | Where-Object { $_.Label -eq 'empty catch block' })
$emptyCatchHits = @()
if ($emptyCatchPattern.Count -gt 0) {
    $emptyCatchHits = @($emptyCatchPattern[0].Hits)
}
if ($emptyCatchHits.Count -gt 0) {
    $findings.Add([pscustomobject]@{
        Severity = 'P1'
        Location = ($emptyCatchHits[0] -split ':')[0..1] -join ':'
        Issue = 'Swallowed exception block detected'
        Impact = 'Runtime errors become silent and hard to diagnose'
        Fix = 'Capture exception and route to Logger with contextual metadata'
    }) | Out-Null
}

$asyncPattern = @($patterns | Where-Object { $_.Label -eq 'async void' })
$asyncHits = @()
if ($asyncPattern.Count -gt 0) {
    $asyncHits = @($asyncPattern[0].Hits)
}
if ($asyncHits.Count -gt 0) {
    foreach ($hit in $asyncHits | Select-Object -First 2) {
        $loc = ($hit -split ':')[0..1] -join ':'
        $findings.Add([pscustomobject]@{
            Severity = 'P1'
            Location = $loc
            Issue = 'async void command/event chain in viewmodel'
            Impact = 'Unhandled exceptions bypass normal flow and are harder to test'
            Fix = 'Use async Task command handlers and central exception pipeline'
        }) | Out-Null
    }
}

$msgPattern = @($patterns | Where-Object { $_.Label -eq 'MessageBox usage' })
$msgBoxHits = @()
if ($msgPattern.Count -gt 0) {
    $msgBoxHits = @($msgPattern[0].Hits | Where-Object {
            $_ -match 'Synthesis\\Core\\Abstraction|Synthesis\\Feature\\SkinEditor\\SkinRepository'
        })
}
if ($msgBoxHits.Count -gt 0) {
    foreach ($hit in $msgBoxHits | Select-Object -First 3) {
        $loc = ($hit -split ':')[0..1] -join ':'
        $findings.Add([pscustomobject]@{
            Severity = 'P1'
            Location = $loc
            Issue = 'UI interaction inside repository/core data path'
            Impact = 'Layering leakage, difficult unit testing and reuse'
            Fix = 'Move interaction to viewmodel/service; repositories return result objects or domain errors'
        }) | Out-Null
    }
}

$baseRepoMessageBoxHits = @($msgBoxHits | Where-Object { $_ -match 'Synthesis\\Core\\Abstraction\\BaseRepository\.cs' })
if ($baseRepoMessageBoxHits.Count -gt 0) {
    $findings.Add([pscustomobject]@{
        Severity = 'P1'
        Location = ($baseRepoMessageBoxHits[0] -split ':')[0..1] -join ':'
        Issue = 'Repository base class displays MessageBox directly'
        Impact = 'Data access layer depends on UI and cannot be cleanly reused/tested'
        Fix = 'Replace popup calls with structured error returns/logging; let ViewModel decide UI prompts'
    }) | Out-Null
}

$skinRepoMessageBoxHits = @($msgBoxHits | Where-Object { $_ -match 'Synthesis\\Feature\\SkinEditor\\SkinRepository\.cs' })
if ($skinRepoMessageBoxHits.Count -gt 0) {
    $findings.Add([pscustomobject]@{
        Severity = 'P1'
        Location = ($skinRepoMessageBoxHits[0] -split ':')[0..1] -join ':'
        Issue = 'SkinRepository performs save-time interactive UI decisions'
        Impact = 'Repository side effects complicate batch processing and error automation'
        Fix = 'Move user decision flow to ViewModel/service and keep repository side-effect free'
    }) | Out-Null
}

$catchWithoutVarPattern = @($patterns | Where-Object { $_.Label -eq 'catch without exception variable' })
$catchWithoutVarHits = @()
if ($catchWithoutVarPattern.Count -gt 0) {
    $catchWithoutVarHits = @($catchWithoutVarPattern[0].Hits)
}
$catchWithoutVarUnity = @($catchWithoutVarHits | Where-Object { $_ -match 'Synthesis\\Core\\Tools\\UnityRichTextHelper\.cs' })
if ($catchWithoutVarUnity.Count -gt 0) {
    $findings.Add([pscustomobject]@{
        Severity = 'P1'
        Location = ($catchWithoutVarUnity[0] -split ':')[0..1] -join ':'
        Issue = 'Catch block without exception variable in UnityRichTextHelper'
        Impact = 'Color parsing failures are silent and diagnostics are lost'
        Fix = 'Log parse failures at debug level with input context, then fallback safely'
    }) | Out-Null
}

$projectManagerFile = $largestFiles | Where-Object { $_.Path -eq 'Synthesis\\Core\\ProjectManager.cs' } | Select-Object -First 1
if ($projectManagerFile -and $projectManagerFile.Lines -ge 200) {
    $findings.Add([pscustomobject]@{
        Severity = 'P2'
        Location = 'Synthesis/Core/ProjectManager.cs'
        Issue = 'Project orchestration class is oversized and highly coupled'
        Impact = 'Change amplification across modules and weak isolation for testing'
        Fix = 'Split loading/saving/artwork scan concerns into dedicated services'
    }) | Out-Null
}

if ($largestFiles[0].Lines -ge 350) {
    $findings.Add([pscustomobject]@{
        Severity = 'P2'
        Location = $largestFiles[0].Path
        Issue = 'Very large file with concentrated responsibilities'
        Impact = 'High maintenance cost and regression probability'
        Fix = 'Split into focused services/helpers and keep viewmodel slim'
    }) | Out-Null
}

if (-not $hasEditorConfig -or -not $hasDirectoryBuildProps) {
    $findings.Add([pscustomobject]@{
        Severity = 'P2'
        Location = 'Project root'
        Issue = 'Missing quality configuration baseline'
        Impact = 'Inconsistent diagnostics across environments'
        Fix = 'Add .editorconfig + Directory.Build.props with analyzer policy'
    }) | Out-Null
}

if ($testProjects.Count -eq 0) {
    $findings.Add([pscustomobject]@{
        Severity = 'P2'
        Location = 'Solution'
        Issue = 'No automated test project detected'
        Impact = 'Weak regression safety net for refactors and hotfixes'
        Fix = 'Introduce tests for core parsing/repository behavior and critical workflows'
    }) | Out-Null
}

$openQuestions = @(
    '- Should repository-layer user prompts be migrated to ViewModel immediately (batch 1), or behind a staged adapter?',
    '- For async command migration, do we standardize on Prism Async DelegateCommand wrappers in this cycle?',
    '- Is strict DLL parity required after every remediation commit, or only at batch boundaries?'
)

$report = New-Object System.Collections.Generic.List[string]
$report.Add('# Quality Audit Report') | Out-Null
$report.Add('') | Out-Null
$report.Add("- Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')") | Out-Null
$report.Add("- Branch: $branch") | Out-Null
$report.Add(("- Baseline SHA: {0} ({1})" -f $head, $headShort)) | Out-Null
$report.Add("- Working tree dirty: $([bool]($status))") | Out-Null
$report.Add('') | Out-Null
$report.Add('## Gate A — Baseline Freeze') | Out-Null
$report.Add(("- Baseline metadata written to {0}" -f $baselinePath)) | Out-Null
$report.Add("- Assembly parity: $($parity.Message)") | Out-Null
if ($parity.Enabled) {
    $report.Add("- Type diff count: $($parity.TypeDiffCount)") | Out-Null
    $report.Add("- Critical type method diff count: $($parity.MethodDiffCount)") | Out-Null
    if ($parity.CriticalTypeDiff.Count -gt 0) {
        $report.Add('- Critical type diffs:') | Out-Null
        foreach ($line in $parity.CriticalTypeDiff) { $report.Add("  - $line") | Out-Null }
    }
}
$report.Add('') | Out-Null

$report.Add('## Gate B — Automated Static Audit') | Out-Null
$report.Add("- Build failed: $buildFailed") | Out-Null
$report.Add("- Warning count: $($warningLines.Count)") | Out-Null
$report.Add("- Error count: $($errorLines.Count)") | Out-Null
$report.Add('') | Out-Null
if ($errorLines.Count -gt 0) {
    $report.Add('### Top Errors (first 20)') | Out-Null
    foreach ($line in ($errorLines | Select-Object -First 20)) {
        $report.Add("- $line") | Out-Null
    }
    $report.Add('') | Out-Null
}
if ($warningLines.Count -gt 0) {
    $report.Add('### Top Warnings (first 20)') | Out-Null
    foreach ($line in ($warningLines | Select-Object -First 20)) {
        $report.Add("- $line") | Out-Null
    }
    $report.Add('') | Out-Null
}
$report.Add('### Pattern Scan Summary') | Out-Null
foreach ($p in $patterns) {
    $report.Add("- $($p.Label): $($p.Count)") | Out-Null
}
$report.Add('') | Out-Null
$report.Add('### Largest Files (Top 10)') | Out-Null
foreach ($r in $largestFiles | Select-Object -First 10) {
    $report.Add("- $($r.Path): $($r.Lines) lines") | Out-Null
}
$report.Add('') | Out-Null
$report.Add('### Method-Heavy Files (Top 10)') | Out-Null
foreach ($r in $methodHeavy | Select-Object -First 10) {
    $report.Add("- $($r.Path): $($r.Methods) methods") | Out-Null
}
$report.Add('') | Out-Null
$report.Add('### Project Quality Baseline') | Out-Null
$report.Add("- .editorconfig present: $hasEditorConfig") | Out-Null
$report.Add("- Directory.Build.props present: $hasDirectoryBuildProps") | Out-Null
$report.Add("- Test projects found: $($testProjects.Count)") | Out-Null
$report.Add('') | Out-Null

$report.Add('## Gate C — Manual Deep Review Targets') | Out-Null
$report.Add('- Repository/domain layering: check MessageBox usage in Core/Repository paths') | Out-Null
$report.Add('- Error handling observability: replace silent catches and popup-only failures with structured logging') | Out-Null
$report.Add('- Concurrency/lifecycle: migrate async void command paths to async Task patterns') | Out-Null
$report.Add('- Maintainability hotspots: split SkinEditorViewModel/SkinEditorView code-behind and simplify orchestration in ProjectManager') | Out-Null
$report.Add('') | Out-Null

$report.Add('## Findings') | Out-Null
$report.Add('Format: `P级 | 文件:行 | 问题 | 影响 | 建议修复`') | Out-Null
foreach ($f in $findings | Sort-Object Severity, Location) {
    $report.Add("- $($f.Severity) | $($f.Location) | $($f.Issue) | $($f.Impact) | $($f.Fix)") | Out-Null
}
if ($findings.Count -eq 0) {
    $report.Add('- No findings generated by current ruleset.') | Out-Null
}
$report.Add('') | Out-Null

$report.Add('## Open Questions') | Out-Null
foreach ($q in $openQuestions) { $report.Add($q) | Out-Null }
$report.Add('') | Out-Null

$report.Add('## Remediation Plan') | Out-Null
$report.Add('1. Batch 1 (P0/P1): compile blockers, exception visibility, async command safety, remove repository-layer UI prompts') | Out-Null
$report.Add('2. Batch 2 (P2): split oversized classes, reduce orchestration coupling, add result/error transport contracts') | Out-Null
$report.Add('3. Batch 3 (P3): naming/style consistency and low-risk cleanup') | Out-Null
$report.Add('') | Out-Null
$report.Add('### Regression and Acceptance') | Out-Null
$report.Add('- Rebuild Release for both projects after each batch') | Out-Null
$report.Add('- Re-run audit script and compare finding counts') | Out-Null
$report.Add('- Re-run DLL parity checks at batch boundary commits') | Out-Null

$report | Set-Content -Encoding utf8 $latestReport
$report | Set-Content -Encoding utf8 $versionedReport

Write-Info "Report generated: $latestReport"
Write-Info "Report snapshot: $versionedReport"
Write-Info "Baseline metadata: $baselinePath"
