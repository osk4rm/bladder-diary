<#
.SYNOPSIS
    Fails when the CI workflow duplicates commands that belong in scripts/ci.

.DESCRIPTION
    `AGENTS.md` calls `scripts/ci/` the single source of truth for how this repository is
    built, linted and tested. That is only true if CI runs the scripts. When the workflow
    repeats their commands instead, the scripts can rot silently: a developer follows the
    documented command, CI runs a different one, and nobody notices until the documentation is
    the only thing that was ever wrong.

    This check enforces the rule mechanically, because a rule that depends on someone
    remembering is not a rule.

    It looks for direct invocations of the tools the scripts own:
      - `dotnet build`, `dotnet test`, `dotnet format`
      - `npm run <script>`, `npm test`, `npm ci` (the scripts own dependency install too)

    Running a script through `pwsh -File scripts/ci/...` is always allowed.
#>
[CmdletBinding()]
param(
    [string] $Workflow = '.github/workflows/ci.yml'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Workflow)) {
    Write-Host "[FAIL] $Workflow does not exist"
    exit 1
}

# Comments explain decisions; they must not trigger the check.
$lines = Get-Content -LiteralPath $Workflow
$offences = New-Object System.Collections.Generic.List[string]

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^\s*#') { continue }
    if ($line -match 'scripts/ci/') { continue }
    # Only `run:` commands execute. A step's `name:` merely describes it, and matching those
    # would fail on a description that happens to mention `dotnet format`.
    if ($line -notmatch '^\s*(-\s*)?run:') { continue }

    if ($line -match 'dotnet\s+(build|test|format)\b') {
        $offences.Add("line $($i + 1): direct dotnet command - run scripts/ci/*.ps1 instead`n    $($line.Trim())")
    }
    elseif ($line -match '\bnpm\s+(run\s+\S+|test|ci|install)\b' -and $line -notmatch 'npm --prefix apps/web run test:e2e') {
        $offences.Add("line $($i + 1): direct npm command - run scripts/ci/*.ps1 instead`n    $($line.Trim())")
    }
}

if ($offences.Count -gt 0) {
    Write-Host ''
    Write-Host "CI duplicates commands that scripts/ci owns ($($offences.Count) offence(s)):" -ForegroundColor Red
    foreach ($offence in $offences) { Write-Host "  [FAIL] $offence" -ForegroundColor Red }
    Write-Host ''
    Write-Host 'Either call the script, or add the command to a script and call it from there.'
    exit 1
}

Write-Host 'CI runs the scripts; no duplicated commands.' -ForegroundColor Green
exit 0
