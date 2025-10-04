#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Apply branch protection rules to the master branch.

.DESCRIPTION
    This script configures branch protection for the master branch according to
    the TelegramYtDlpBot Constitution v1.1.0 requirement that NO changes shall
    be committed directly to the main branch.

.PARAMETER DryRun
    Show what would be done without actually applying changes.

.EXAMPLE
    .\apply-branch-protection.ps1
    Apply branch protection to master branch

.EXAMPLE
    .\apply-branch-protection.ps1 -DryRun
    Show what would be applied without making changes
#>

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

# Check if GitHub CLI is installed
try {
    $ghVersion = gh --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub CLI not found"
    }
    Write-Host "✓ GitHub CLI detected: $($ghVersion[0])" -ForegroundColor Green
} catch {
    Write-Error "GitHub CLI (gh) is not installed. Install from: https://cli.github.com/"
    exit 1
}

# Check if authenticated
try {
    gh auth status 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Not authenticated"
    }
    Write-Host "✓ GitHub CLI authenticated" -ForegroundColor Green
} catch {
    Write-Error "Not authenticated with GitHub CLI. Run: gh auth login"
    exit 1
}

$repo = "taldelarosa/TelegramYtDlpBot"
$branch = "master"

Write-Host "`nConfiguring branch protection for $repo branch '$branch'..." -ForegroundColor Cyan

# Branch protection payload
$protection = @{
    required_status_checks = $null  # Will add when CI/CD is implemented
    enforce_admins = $true
    required_pull_request_reviews = @{
        required_approving_review_count = 1
        dismiss_stale_reviews = $true
        require_code_owner_reviews = $false
    }
    restrictions = $null
    allow_force_pushes = $false
    allow_deletions = $false
    required_conversation_resolution = $true
    lock_branch = $false
    required_linear_history = $false
} | ConvertTo-Json -Depth 10

if ($DryRun) {
    Write-Host "`n[DRY RUN] Would apply the following protection:" -ForegroundColor Yellow
    Write-Host $protection -ForegroundColor Gray
    Write-Host "`nTo apply for real, run without -DryRun flag" -ForegroundColor Yellow
    exit 0
}

try {
    # Apply branch protection
    Write-Host "`nApplying branch protection rules..." -ForegroundColor Cyan
    $protection | gh api "repos/$repo/branches/$branch/protection" -X PUT --input -
    
    Write-Host "`n✓ Branch protection successfully applied!" -ForegroundColor Green
    Write-Host "`nProtection rules enforced:" -ForegroundColor Cyan
    Write-Host "  • Require pull request reviews (1 approval minimum)" -ForegroundColor White
    Write-Host "  • Dismiss stale reviews on new commits" -ForegroundColor White
    Write-Host "  • Require conversation resolution before merge" -ForegroundColor White
    Write-Host "  • Prevent force pushes" -ForegroundColor White
    Write-Host "  • Prevent branch deletion" -ForegroundColor White
    Write-Host "  • Enforce for administrators" -ForegroundColor White
    
    Write-Host "`nVerify at: https://github.com/$repo/settings/branches" -ForegroundColor Gray
    
} catch {
    Write-Error "Failed to apply branch protection: $_"
    Write-Host "`nYou can also configure manually at:" -ForegroundColor Yellow
    Write-Host "https://github.com/$repo/settings/branches" -ForegroundColor Gray
    exit 1
}
