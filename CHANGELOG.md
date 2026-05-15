# Changelog

[keep-a-changelog]: https://keepachangelog.com/en/1.1.0/
[semver]: https://semver.org/spec/v2.0.0.html

All notable changes to the Fellhaven project will be documented in this file.

The format is based on [Keep a Changelog][keep-a-changelog],
and this project adheres to [Semantic Versioning][semver].

## [Unreleased]

### Added
- Initial project structure and GitHub repository setup
- Core graph-based dungeon model (Location, Edge, Direction classes)
- Lua scripting engine integration (MoonSharp)
- Localization system with English and Hungarian translations
- Dungeon loader for JSON-based dungeon definitions
- GameState management (flags, variables, inventory, party)
- Example test dungeon with locked door mechanic
- Comprehensive documentation (ADRs, GDD, API docs)
- Unit test framework setup (NUnit)
- GitHub Actions CI/CD workflow
- Issue and PR templates

### Architecture Decisions
- ADR-001: Graph-Based Dungeon Model
- ADR-002: Lua Scripting for Conditions and Actions
- ADR-003: JSON-Based Localization System

## [0.1.0-alpha] - 2026-05-14

### Added
- Initial project setup
- Core systems foundation
- Development documentation

---

## Version History

- **0.1.0-alpha** (2026-05-14): Initial setup with core architecture

---

## Legend

- **Added**: New features
- **Changed**: Changes to existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security vulnerability fixes
