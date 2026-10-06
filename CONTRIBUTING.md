# Contributing to STB StageConnect

Thank you for your interest in contributing! This document explains how to participate in this project effectively.

---

## Branch Strategy

| Branch | Purpose |
|:---|:---|
| `main` | Production-ready, stable code. Protected — no direct pushes. |
| `develop` | Integration branch for completed features. PRs merge here first. |
| `feature/<name>` | Individual feature development. Branch off from `develop`. |
| `fix/<name>` | Bug fixes. Branch off from `develop` (or `main` for hotfixes). |
| `docs/<name>` | Documentation changes only. |

```bash
# Create a feature branch
git checkout develop
git pull origin develop
git checkout -b feature/my-new-feature
```

---

## Commit Message Convention

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short summary>

[optional body]

[optional footer]
```

**Types:**

| Type | When to use |
|:---|:---|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `style` | Code style (formatting, missing semi-colons, etc.) |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test` | Adding or fixing tests |
| `chore` | Build process, dependency updates, tooling |
| `perf` | Performance improvement |

**Examples:**

```bash
git commit -m "feat(stagiaire-service): add weekly logbook endpoint"
git commit -m "fix(notification-service): correct SignalR hub group scoping"
git commit -m "docs: update Quick Start environment variables table"
```

---

## Pull Request Process

1. Make sure your branch is up to date with `develop`:
   ```bash
   git fetch origin
   git rebase origin/develop
   ```
2. Run the build locally before pushing:
   ```bash
   dotnet build
   mvn clean verify -B   # for Java services
   ```
3. Open a PR against **`develop`** (not `main`)
4. Fill in the PR template completely
5. Ensure the CI pipeline passes (GitHub Actions)
6. Request a review from at least one maintainer

---

## Code Style

- **C# / .NET:** Follow standard C# conventions; use `var` when the type is obvious; async/await throughout; XML doc comments on public APIs
- **Java / Spring:** Follow Google Java Style Guide; Lombok for boilerplate; constructor injection preferred
- **Angular / TypeScript:** Standalone components, signal-based inputs/outputs, `OnPush` change detection
- **Commit Scope:** Use the service name as scope (e.g., `stagiaire-service`, `notification-service`, `frontend`, `docker`, `k8s`)

---

## Local Development Setup

See [README.md](./README.md#-quick-start) for the full Docker Compose setup.

For developing a single .NET service:

```bash
cd Stagiaire.Service
dotnet restore
dotnet run
```

For the Angular frontend:

```bash
cd Frontend/angular-app
npm install
npm run start
```

*STB StageConnect — Engineering Internship Project (STB)*
*Author: Abdelkader Kaddachi*
