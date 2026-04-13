# Versioning Strategy

## Scheme

**Semantic Versioning** (SemVer): `MAJOR.MINOR.PATCH`

| Component | Bumped when | Examples |
|-----------|------------|---------|
| MAJOR | Game-breaking changes, save format incompatibility, major milestone | 0 → 1 = first release |
| MINOR | New features, systems, content additions | 0.1 → 0.2 = new system added |
| PATCH | Bug fixes, balance tweaks, polish | 0.1.0 → 0.1.1 = bug fix |

**Pre-1.0:** All development is `0.x.y`. MINOR bumps for features, PATCH for fixes. No stability guarantees until 1.0.

## Source of Truth

**Git tags.** Version is derived from the latest git tag matching `v*`.

```bash
# Tag a release
git tag v0.12.0
git push origin v0.12.0

# Read current version
git describe --tags --abbrev=0
```

## Conventions

- Tags use `v` prefix: `v0.1.0`, `v0.12.3`, `v1.0.0`
- Tag on `main` branch only (after merge)
- Every tag gets a CHANGELOG.md entry
- CI can read the tag for build metadata / export versioning
- No version hardcoded in code — read from `ProjectSettings` or environment at runtime if needed

## Current Version

Pre-release development. No tags yet. First tag will be `v0.1.0` when the testing infrastructure is stable and the first playable loop is verified.

## Mapping to Dev Phases

| Phase | Version range | Status |
|-------|-------------|--------|
| Phase 0 (Visual Foundation) | — | Done, pre-versioning |
| Phase 0.5 (Playable Prototype) | — | Done, pre-versioning |
| Phase 1 (Complete Systems) | v0.1.0 – v0.9.x | In progress |
| Phase 2 (Endgame & Polish) | v0.10.0+ | Planned |
| Release | v1.0.0 | Ship |
