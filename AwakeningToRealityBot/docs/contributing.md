# Contributing Guidelines

## Development workflow

1. Fork the repository and create feature branches from `main`.
2. Keep pull requests small (target < 500 lines) and focused on a single concern.
3. Ensure CI pipelines pass locally before opening a pull request.

## Coding standards

- Target **.NET 8**.  Avoid legacy frameworks unless explicitly required.
- Prefer dependency injection over static singletons.
- Validate all external input with FluentValidation or equivalent.
- Write unit tests for business logic and integration tests for infrastructure boundaries.

## Commit hygiene

- Use [Conventional Commits](https://www.conventionalcommits.org/).
- Include context in commit bodies when the change is not self-explanatory.
- Never commit secrets or binary artefacts (`bin`, `obj`, `.vs`, `*.user`).

## Review checklist

- [ ] Tests added or updated.
- [ ] Documentation updated where behaviour changes.
- [ ] App Configuration / infrastructure changes documented.
- [ ] Screenshots attached for UI changes.
