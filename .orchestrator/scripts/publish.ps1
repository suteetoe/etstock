<#
.SYNOPSIS
    Publish an already-committed feature branch: push + open exactly one PR via gh.

.DESCRIPTION
    Use this when delegate.ps1 ran WITHOUT -Publish (so the work is committed locally but not
    yet pushed). This is a remote-affecting step - per orchestrator rules, run it only after the
    human has confirmed. It pushes the branch, opens one PR against the default branch using the
    standard body template, and records the task -> PR mapping in state.json.

.EXAMPLE
    .\publish.ps1 -Branch "backend/inventory-add-product-entity" `
        -Title "[backend] Add Product entity + DbContext" -FeatureId F001
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string] $Branch,
    [Parameter(Mandatory)][string] $Title,
    [string] $Body,
    [string] $FeatureId,
    [string] $Group = 'backend',
    [string] $Task,
    [string] $Scope = '(see commit)'
)

. "$PSScriptRoot\lib.ps1"
Assert-Tool 'git'
Assert-Tool 'gh'

$state = Get-State
$base  = $state.project.defaultBranch
if ($Branch -eq $base) { throw "Refusing to publish the default branch '$base'." }

if (-not $Body) {
    $taskText = if ($Task) { $Task } else { $Title }
    $Body = Build-PrBody -Group $Group -Task $taskText -Scope $Scope -FeatureId $FeatureId -Provider '(committed' -Model 'locally)'
}

if ($PSCmdlet.ShouldProcess("origin/$Branch", "git push -u")) {
    git -C $RepoRoot push -u origin $Branch
}

if ($PSCmdlet.ShouldProcess("PR $Branch -> $base", "gh pr create")) {
    Push-Location $RepoRoot
    try {
        $url = gh pr create --base $base --head $Branch --title $Title --body $Body
        Write-Host "PR created: $url" -ForegroundColor Green
        Record-Pr -State $state -Url $url -Branch $Branch -Title $Title -FeatureId $FeatureId
    }
    finally { Pop-Location }
}
