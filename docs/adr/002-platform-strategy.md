[adr-001]: 001-engine-choice.md
[adr-003]: 003-graph-based-dungeon.md

# ADR-002: Multi-Platform Development Strategy

**Status**: Accepted

**Date**: 2026-05-15

## Context

The project aims to support three major platform targets:
- Desktop (Windows, Linux, macOS)
- Web (HTML5/WebGL)
- Mobile (iOS, Android)

This requires thoughtful architecture to maximize code reuse while allowing platform-specific optimizations.

## Decision

We will use a **platform-agnostic core** with **platform-specific adaptation layers**:

1. **Core Gameplay Logic**: Platform-independent C# code
2. **Platform Detector**: Single abstraction for platform detection
3. **Input Handler**: Platform-specific input mapping (keyboard/mouse → touch)
4. **UI Scaling**: Responsive design that adapts to screen sizes
5. **Asset Pipeline**: Unified asset management with platform-specific variants

## Rationale

1. **Code Reuse**: 80-90% of code remains platform-agnostic.
2. **Flexibility**: Easy to add platform-specific optimizations without affecting core logic.
3. **Maintainability**: Clear separation of concerns reduces complexity.
4. **Testing**: Core logic can be tested independently of platforms.
5. **Performance**: Allow platform-specific tuning (web workers, mobile optimization, etc.).

## Consequences

### Positive
- Single codebase across all platforms
- Easy to add new platforms in the future
- Clear testing strategy
- Flexible deployment pipeline

### Negative
- Additional abstraction layer adds slight complexity
- Must maintain platform adapters as platforms evolve
- Some platform-specific features harder to implement

### Neutral
- Input abstraction may require remapping for each platform
- UI scaling requires responsive design consideration

## Alternatives Considered

1. **Three Separate Projects**: Easier initially but massive code duplication and maintenance burden.
2. **Platform-Specific Branches**: Diverges over time, difficult to merge.
3. **Monolithic Platform Handling**: Spaghetti code with mixed concerns.

## Related ADRs

- [ADR-001: Engine Choice][adr-001]
- [ADR-003: Graph-Based Dungeon Model][adr-003]

---

**Owner**: Project Lead  
**Last Updated**: 2026-05-15
