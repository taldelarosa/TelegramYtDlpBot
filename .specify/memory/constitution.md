<!--
Sync Impact Report:
- Version change: none → 1.0.0 (initial constitution)
- Added principles: Test-Driven Development, Code Quality Standards, Security First, Performance Requirements, User Experience Consistency
- Added sections: Technology Standards, Development Workflow
- Templates requiring updates:
  ✅ plan-template.md - Updated Constitution Check section with concrete principles, updated version reference to v1.0.0
  ✅ spec-template.md - Already aligned (testable requirements, security, performance considerations present)
  ✅ tasks-template.md - Already aligned (TDD workflow, security tasks, performance tests present)
  ✅ agent-file-template.md - Template structure supports constitutional requirements
- Command prompts reviewed:
  ✅ plan.prompt.md - References constitution.md correctly
  ✅ All other command prompts - No updates needed
- Follow-up TODOs: none
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

## Development Workflow

All changes MUST be implemented on feature branches with descriptive names. Pull requests MUST include test coverage reports and pass all automated checks. Code reviews are REQUIRED and MUST verify adherence to all constitutional principles. Database schema changes MUST include migration scripts and rollback procedures.

## Governance

This constitution supersedes all other development practices and coding standards. All pull requests and code reviews MUST verify compliance with these principles. Any deviation MUST be explicitly justified and documented. Amendments require team consensus and MUST include impact analysis and migration plan for existing code.

**Version**: 1.0.0 | **Ratified**: 2025-10-04 | **Last Amended**: 2025-10-04