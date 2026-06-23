<#
.SYNOPSIS
    Poll open PRs and surface the ones the orchestrator hasn't reviewed yet.

.DESCRIPTION
    Lists open PRs via gh, compares each PR's head SHA against the lastReviewedSha recorded
    in state.json, and reports which PRs need review. For a chosen PR (-Number) it prints the
    diff + checks so the orchestrator can review it. Read-only: never merges, never pushes.

    Designed to be replaced later by an event-driven GitHub Actions trigger; the state.json
    contract (prs[number].lastReviewedSha / verdict / rounds) stays the same.

.EXAMPLE
    .\review-poll.ps1                 # list PRs needing review
.EXAMPLE
    .\review-poll.ps1 -Number 7       # show diff + checks for PR #7
.EXAMPLE
    .\review-poll.ps1 -Number 7 -MarkReviewed -Verdict approved
#>
[CmdletBinding()]
param(
    [int] $Number,
    [switch] $MarkReviewed,
    [ValidateSet('approved','changes-requested','pending')][string] $Verdict = 'pending'
)

. "$PSScriptRoot\lib.ps1"
Assert-Tool 'gh'
Push-Location $RepoRoot
try {
    $state = Get-State

    # ---- detail view for one PR ---------------------------------------------
    if ($Number) {
        $info = gh pr view $Number --json number,title,headRefName,headRefOid,state,url | ConvertFrom-Json
        Write-Host "=== PR #$($info.number): $($info.title) ===" -ForegroundColor Cyan
        Write-Host "Branch : $($info.headRefName)"
        Write-Host "Head   : $($info.headRefOid)"
        Write-Host "State  : $($info.state)   $($info.url)"

        $recorded = $state.prs.$("$Number")
        if ($recorded) {
            Write-Host "Recorded verdict: $($recorded.verdict)  rounds: $($recorded.rounds)  lastReviewedSha: $($recorded.lastReviewedSha)"
        } else {
            Write-Host "Recorded verdict: (not in state.json yet)"
        }

        Write-Host "`n--- CHECKS ---" -ForegroundColor Cyan
        gh pr checks $Number 2>&1 | Out-Host

        Write-Host "`n--- DIFF ---" -ForegroundColor Cyan
        gh pr diff $Number | Out-Host

        if ($MarkReviewed) {
            $prObj = if ($recorded) { $recorded } else {
                [pscustomobject]@{ branch=$info.headRefName; feature=$null; title=$info.title; url=$info.url; lastReviewedSha=$null; verdict='pending'; rounds=0; reviewedAt=$null }
            }
            $prObj.lastReviewedSha = $info.headRefOid
            $prObj.verdict         = $Verdict
            $prObj.reviewedAt      = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
            $state.prs | Add-Member -NotePropertyName "$Number" -NotePropertyValue $prObj -Force
            Save-State $state
            Write-Host "`nMarked PR #$Number as '$Verdict' at SHA $($info.headRefOid)." -ForegroundColor Green
        }
        return
    }

    # ---- list view: which PRs need review -----------------------------------
    $open = gh pr list --state open --json number,title,headRefName,headRefOid,isDraft | ConvertFrom-Json
    if (-not $open -or $open.Count -eq 0) { Write-Host "No open PRs." -ForegroundColor Yellow; return }

    Write-Host "=== OPEN PRs ===" -ForegroundColor Cyan
    foreach ($pr in $open) {
        $rec = $state.prs.$("$($pr.number)")
        $needs = (-not $rec) -or ($rec.lastReviewedSha -ne $pr.headRefOid) -or ($rec.verdict -eq 'pending')
        $status = if (-not $rec) { 'NEW - needs review' }
                  elseif ($rec.lastReviewedSha -ne $pr.headRefOid) { "UPDATED since review (round $($rec.rounds)) - needs re-review" }
                  elseif ($rec.verdict -eq 'pending') { 'pending' }
                  else { "reviewed: $($rec.verdict)" }
        $color = if ($needs) { 'Yellow' } else { 'Gray' }
        $draft = if ($pr.isDraft) { ' [draft]' } else { '' }
        Write-Host ("  PR #{0,-4} {1}{2}`n        {3}" -f $pr.number, $pr.title, $draft, $status) -ForegroundColor $color
    }
    Write-Host "`nReview one with:  .\review-poll.ps1 -Number <n>" -ForegroundColor Cyan
}
finally {
    Pop-Location
}
