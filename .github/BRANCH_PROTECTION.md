# Branch Protection Policy

This directory contains Infrastructure as Code (IaC) for the TelegramYtDlpBot repository's branch protection rules.

## Constitutional Requirement

Per **TelegramYtDlpBot Constitution v1.1.0**, Section "Development Workflow":

> NO changes SHALL be committed directly to the main branch - all work MUST go through the branch and merge workflow.

## Applying Branch Protection

### Automated (Recommended)

Use the provided PowerShell script:

```powershell
# Dry run to see what would be applied
.\.github\scripts\apply-branch-protection.ps1 -DryRun

# Apply branch protection
.\.github\scripts\apply-branch-protection.ps1
```

**Prerequisites:**
- GitHub CLI (`gh`) installed and authenticated
- Repository admin permissions

### Manual Configuration

1. Go to: https://github.com/taldelarosa/TelegramYtDlpBot/settings/branches
2. Click "Add branch protection rule"
3. Branch name pattern: `master`
4. Enable the following:
   - ✅ Require a pull request before merging
     - Required approvals: 1
     - Dismiss stale pull request approvals when new commits are pushed
   - ✅ Require conversation resolution before merging
   - ✅ Do not allow bypassing the above settings (enforce for administrators)
   - ✅ Restrict deletions
   - ✅ Block force pushes
5. Click "Create" or "Save changes"

## Protection Rules

The following rules are enforced:

| Rule | Setting | Rationale |
|------|---------|-----------|
| Require pull request reviews | 1 approval | Ensures peer review per constitution |
| Dismiss stale reviews | Enabled | New code requires new review |
| Require conversation resolution | Enabled | All feedback must be addressed |
| Prevent force pushes | Enabled | Maintains clean audit trail |
| Prevent branch deletion | Enabled | Protects main branch |
| Enforce for admins | Enabled | Constitution applies to everyone |

## Future Enhancements

When CI/CD is implemented, update the protection to require:
- ✅ All status checks must pass
- ✅ Build successful
- ✅ All tests passing
- ✅ Code coverage threshold met

## Verification

After applying, verify at:
- Web UI: https://github.com/taldelarosa/TelegramYtDlpBot/settings/branches
- CLI: `gh api repos/taldelarosa/TelegramYtDlpBot/branches/master/protection`

## Troubleshooting

**"GitHub CLI not found"**
- Install from: https://cli.github.com/

**"Not authenticated"**
- Run: `gh auth login`

**"404 Not Found" or "403 Forbidden"**
- Ensure you have admin permissions on the repository
- Verify the repository exists and you're authenticated as the correct user
