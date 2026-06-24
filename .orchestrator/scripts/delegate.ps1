<#
.SYNOPSIS
    Delegate a scoped task to a sub-agent CLI, enforcing 1 task = 1 PR.

.DESCRIPTION
    The SCRIPT owns every git/PR step so a PR is guaranteed and consistently formatted -
    it does NOT rely on the agent to run git. Pipeline:

      a. git checkout -b <group>/<feature>-<desc>   (from origin/<default>)
      b. run the sub-agent CLI to edit ONLY the in-scope files (agent must not touch git)
      c. git add -A && git commit                   (script commits the agent's work)
      d. git push -u origin <branch>                (only with -Publish)
      e. gh pr create (title + body template)       (only with -Publish)

    Steps d+e affect the remote, so they are gated behind -Publish. Per orchestrator rules
    the human must confirm before publishing; run without -Publish first (stops after the
    local commit), then re-run the publish step via publish.ps1, or pass -Publish once the
    human has approved.

.EXAMPLE
    # Produce work + local commit, stop before push (default, safe):
    .\delegate.ps1 -Group backend -Task "Add Product entity + DbContext" `
        -Scope "ETStock/Models/**,ETStock/Data/**" `
        -Branch "backend/inventory-add-product-entity" -FeatureId F001

.EXAMPLE
    # Full pipeline incl. push + PR (only after human approval):
    .\delegate.ps1 -Group backend -Task "..." -Scope "ETStock/Models/**" `
        -Branch "backend/inventory-add-product-entity" -FeatureId F001 -Publish

.EXAMPLE
    .\delegate.ps1 -Group frontend -Task "..." -Scope "ETStock/Views/**" `
        -Branch "frontend/inventory-product-list" -DryRun
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('lead','frontend','backend','qa')][string] $Group,
    [Parameter(Mandatory)][string] $Task,
    [Parameter(Mandatory)][string] $Scope,
    [Parameter(Mandatory)][string] $Branch,
    [ValidateSet('claude','codex')][string] $Provider,
    [string] $Model,
    [ValidateSet('default','fallback')][string] $Tier = 'default',
    [string] $FeatureId,
    [string] $CommitMessage,
    [string] $Title,
    [string] $Body,
    [switch] $Publish,
    [switch] $DryRun
)

. "$PSScriptRoot\lib.ps1"

# --- validate branch name against policy --------------------------------------
$state = Get-State
$pattern = $state.policy.branchPattern
if ($Branch -notmatch $pattern) {
    throw "Branch '$Branch' does not match required pattern: $pattern (e.g. $Group/feature-desc)"
}

# Resolve provider/model from the group's $Tier mapping unless overridden on the CLI.
$map = Get-AgentMapping -Group $Group -Tier $Tier
if (-not $Provider) { $Provider = $map.Provider }
if (-not $Model)    { $Model    = $map.Model }
$Cli = Get-ProviderCli -Provider $Provider
Assert-Tool $Cli
Assert-Tool 'git'
if ($Publish) { Assert-Tool 'gh' }

if (-not $CommitMessage) { $CommitMessage = "$Group`: $Task" }
if (-not $Title)         { $Title = "[$Group] $Task" }
if (-not $Body)          { $Body  = Build-PrBody -Group $Group -Task $Task -Scope $Scope -FeatureId $FeatureId -Provider $Provider -Model $Model }

$prompt = Build-AgentPrompt -Group $Group -Task $Task -Scope $Scope -Branch $Branch -FeatureId $FeatureId

Write-Host "=== DELEGATE (1 task = 1 PR) ===" -ForegroundColor Cyan
Write-Host "Group   : $Group"
Write-Host "Provider: $Provider/$Model ($Tier tier, via $Cli)"
Write-Host "Branch  : $Branch"
Write-Host "Scope   : $Scope"
Write-Host "Feature : $(if ($FeatureId) { $FeatureId } else { '(none)' })"
Write-Host "Publish : $($Publish.IsPresent)"
Write-Host "----------------------------------------"

if ($DryRun) {
    Write-Host "[DryRun] Prompt that would be sent to the agent:`n" -ForegroundColor Yellow
    Write-Host $prompt
    Write-Host "`n[DryRun] PR title : $Title" -ForegroundColor Yellow
    Write-Host "[DryRun] No branch created, no agent invoked, no commit/push/PR." -ForegroundColor Yellow
    return
}

if (-not (Test-CleanWorktree)) {
    throw "Working tree is not clean. Commit/stash changes before delegating."
}

# --- a. create branch from up-to-date default branch --------------------------
$base = $state.project.defaultBranch
Write-Host "[a] Fetching origin and creating branch '$Branch' from origin/$base..." -ForegroundColor Cyan
git -C $RepoRoot fetch origin $base
$exists = git -C $RepoRoot branch --list $Branch
if ($exists) { git -C $RepoRoot checkout $Branch }
else         { git -C $RepoRoot checkout -b $Branch "origin/$base" }

# --- b. invoke the agent (edits files only) -----------------------------------
Write-Host "[b] Running $Provider/$Model via '$Cli' (edits in-scope files only; no git)..." -ForegroundColor Cyan
Push-Location $RepoRoot
try {
    switch ($Provider) {
        'codex'  { codex exec --full-auto -m $Model $prompt }
        'claude' { claude-acp -p $prompt --model $Model }
    }
}
finally { Pop-Location }

# --- c. commit (the script commits, not the agent) ----------------------------
if (Test-CleanWorktree) {
    Write-Warning "No file changes produced by the agent - nothing to commit. Aborting before commit/PR."
    Write-Host "On branch '$Branch'. Investigate the agent output, then re-run or clean up the branch." -ForegroundColor Yellow
    return
}
Write-Host "[c] Committing changes: $CommitMessage" -ForegroundColor Cyan
git -C $RepoRoot add -A
git -C $RepoRoot commit -m $CommitMessage
git -C $RepoRoot --no-pager log --oneline -n 1

# --- d + e. push + PR (gated) -------------------------------------------------
if (-not $Publish) {
    Write-Host "`n[d/e] Skipped push + PR (no -Publish)." -ForegroundColor Yellow
    Write-Host "Local commit is ready on '$Branch'. After human approval, publish with:" -ForegroundColor Yellow
    Write-Host "  .\publish.ps1 -Branch $Branch -Title `"$Title`" -FeatureId `"$FeatureId`"" -ForegroundColor Yellow
    return
}

if ($PSCmdlet.ShouldProcess("origin/$Branch", "git push -u")) {
    Write-Host "[d] Pushing branch..." -ForegroundColor Cyan
    git -C $RepoRoot push -u origin $Branch
}
if ($PSCmdlet.ShouldProcess("PR $Branch -> $base", "gh pr create")) {
    Write-Host "[e] Opening PR..." -ForegroundColor Cyan
    Push-Location $RepoRoot
    try {
        $url = gh pr create --base $base --head $Branch --title $Title --body $Body
        Write-Host "PR created: $url" -ForegroundColor Green
        Record-Pr -State $state -Url $url -Branch $Branch -Title $Title -FeatureId $FeatureId
    }
    finally { Pop-Location }
}
