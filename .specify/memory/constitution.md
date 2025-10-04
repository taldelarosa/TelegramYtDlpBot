<!--
Sync Impact Report:
- Version change: 1.1.0 → 1.2.0 (MINOR - new deployment standards, Git Flow workflow, and quality gates)
- Modified sections:
  * Technology Standards - Added devcontainer and Docker deployment requirements
  * Development Workflow - Updated to Git Flow branching model + mandatory pre-PR linting/testing
- Added sections:
  * Deployment Standards - New section for Docker/Unraid deployment requirements
- Changes summary:
  * Added: VS Code devcontainer requirement for consistent dev environments
  * Added: Docker containerization with multi-stage builds, health checks
  * Added: Unraid deployment standards and Community Applications template requirement
  * Changed: Branch workflow from simple feature branches to full Git Flow model
  * Added: Explicit Git Flow branch types (feature/*, release/*, hotfix/*)
  * Added: develop-gitflow-docker as integration branch, master as production
  * Added: Mandatory pre-PR linting (GitHub-compatible) with auto-fix requirement
  * Added: Mandatory pre-PR testing - all tests must pass before PR creation
  * Added: Branch merge rules for Git Flow (feature→develop, release→master+develop)
- Removed sections: none
- Templates requiring updates:
  ✅ plan-template.md - Update branch naming to feature/*, add deployment check, add lint/test gates
  ✅ spec-template.md - Update branch naming to feature/*
  ✅ tasks-template.md - Add lint/test tasks before PR creation
  ⚠ Constitution version updated to v1.2.0
- Command prompts reviewed:
  ✅ All prompts - Compatible with Git Flow workflow
- Follow-up TODOs:
  * Create .devcontainer configuration
  * Create Dockerfile with multi-stage build
  * Create Unraid template XML
  * Apply branch protection to develop-gitflow-docker branch
  * Set up GitHub Actions for automated linting and testing
-->

# TelegramYtDlpBot Constitution

## Core Principles

### I. Test-Driven Development (NON-NEGOTIABLE)
TDD is mandatory for all code changes. Tests MUST be written before implementation begins. The Red-Green-Refactor cycle is strictly enforced: write failing tests, implement minimal code to pass, then refactor for quality. No code may be merged without corresponding tests that validate its behavior and edge cases.

**Rationale**: TDD ensures design quality, prevents regressions, and provides living documentation of system behavior.

### II. Code Quality Standards
All C# code MUST follow established coding conventions and pass static analysis. PowerShell scripts MUST use approved verbs, proper error handling, and parameter validation. Code MUST be self-documenting with clear naming, appropriate comments for complex logic, and consistent formatting. All public APIs require XML documentation.

**Rationale**: Consistent, readable code reduces maintenance burden and enables effective collaboration.

### III. Security First
NO secrets, API keys, tokens, or sensitive configuration SHALL be committed to version control. Use environment variables, secure configuration providers, or external secret management systems. All external inputs MUST be validated and sanitized. Authentication and authorization MUST be implemented for all bot commands that modify state or access sensitive data.

**Rationale**: Security vulnerabilities can expose user data and compromise system integrity. Prevention is cheaper than remediation.

### IV. Performance Requirements
Bot responses MUST complete within 5 seconds for standard operations and 30 seconds for media downloads. Memory usage MUST be monitored and bounded to prevent resource exhaustion. Database queries MUST be optimized and include appropriate indexes. Large file operations MUST use streaming and provide progress feedback.

**Rationale**: Poor performance directly impacts user experience and can lead to service timeouts or resource exhaustion.

### V. User Experience Consistency
Bot responses MUST use consistent command syntax, error messages, and feedback patterns. All operations MUST provide clear status updates and error explanations. Interactive commands MUST include help text and usage examples. User data and preferences MUST persist across sessions where applicable.

**Rationale**: Consistent UX reduces user confusion and support burden while improving adoption and satisfaction.

## Technology Standards

C# code MUST target .NET 8 or later with nullable reference types enabled. PowerShell scripts MUST be compatible with PowerShell 7+ and follow PSScriptAnalyzer rules. All dependencies MUST be explicitly versioned and regularly updated for security patches. External API integrations MUST implement retry logic with exponential backoff and circuit breaker patterns.

All development MUST occur within VS Code devcontainers to ensure consistent development environments across team members. The devcontainer configuration MUST include all required tools, SDKs, and dependencies. The application MUST be containerized using Docker and deployable to Unraid. Docker images MUST follow best practices: multi-stage builds, minimal base images, non-root users, and proper health checks. Container configuration MUST externalize all environment-specific settings.

**Rationale**: Devcontainers eliminate "works on my machine" issues. Docker containerization ensures consistent deployment and simplifies Unraid installation.

## Development Workflow

Development follows the Git Flow branching model with `master` as the production branch and `develop-gitflow-docker` as the integration branch. Feature development MUST occur on `feature/*` branches created from develop. Release preparation MUST use `release/*` branches. Hotfixes for production issues MUST use `hotfix/*` branches from `master`. NO changes SHALL be committed directly to `master` or `develop-gitflow-docker` - all work MUST go through the branch and merge workflow.

Before creating any pull request, ALL GitHub-compatible linting MUST be run and all issues MUST be auto-fixed or manually resolved. ALL tests MUST pass locally before the PR is initiated. Pull requests MUST include test coverage reports and pass all automated checks before merge. Code reviews are REQUIRED and MUST verify adherence to all constitutional principles. All PR conversations MUST be resolved before merge approval. Feature branches merge to develop, releases merge to both `master` and develop, hotfixes merge to both `master` and develop. Database schema changes MUST include migration scripts and rollback procedures.

**Rationale**: Git Flow provides clear separation between development, release prep, and production code. Pre-PR linting and testing catches issues early and reduces review cycles. Branch protection prevents accidental corruption, ensures peer review, and maintains a clean, auditable history.

## Deployment Standards

The application MUST be packaged as a Docker container suitable for Unraid deployment. Container images MUST be tagged with semantic versions matching Git releases. The Dockerfile MUST implement multi-stage builds to minimize final image size. Health check endpoints MUST be implemented and configured in the container. All configuration MUST use environment variables or mounted configuration files - no secrets in images. Documentation MUST include Unraid template XML for Community Applications integration. Container logs MUST write to stdout/stderr for proper Unraid log aggregation.

**Rationale**: Standardized Docker deployment to Unraid simplifies installation, updates, and ensures consistent runtime environments across user installations.

## Governance

This constitution supersedes all other development practices and coding standards. All pull requests and code reviews MUST verify compliance with these principles. Any deviation MUST be explicitly justified and documented. Amendments require team consensus and MUST include impact analysis and migration plan for existing code.

**Version**: 1.2.0 | **Ratified**: 2025-10-04 | **Last Amended**: 2025-10-04