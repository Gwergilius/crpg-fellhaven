[adr-002]: 002-platform-strategy.md

# ADR-001: Choose Godot 4.6.2 as Game Engine

**Status**: Accepted

**Date**: 2026-05-15

## Context

The Fellhaven CRPG project requires a modern game engine capable of:
- Cross-platform deployment (Desktop, Web, Mobile)
- Turn-based game logic implementation
- C# scripting support
- Strong 2D/3D capability for fantasy aesthetic
- Active community and long-term support
- Minimal licensing restrictions

Several engines were evaluated for this use case.

## Decision

We chose **Godot Engine 4.6.2** with C# scripting as the primary development platform.

## Rationale

1. **Cross-Platform Support**: Godot natively supports Windows, Linux, macOS, Web (HTML5/WebGL), Android, and iOS from a single codebase.

2. **C# Support**: Godot 4.x provides excellent C# integration through the .NET framework, enabling use of .NET 10 and C# 14 (or fallback to .NET 8.0 and C# 12).

3. **Open Source**: MIT licensed, ensuring long-term viability and community contribution.

4. **Visual Flexibility**: Strong support for 2D/3D rendering and custom shaders needed for fantasy CRPG aesthetics.

5. **Turn-Based Friendly**: Godot's scene system and scripting make turn-based games straightforward to implement.

6. **Learning Curve**: Reasonable learning curve for developers familiar with C# and OOP patterns.

7. **Community**: Strong and growing community with excellent documentation and third-party resources.

## Consequences

### Positive
- Single codebase for all platforms
- Strong C# ecosystem integration
- Free and open-source
- Regular updates and long-term support
- Excellent cross-platform export capabilities

### Negative
- Smaller community than Unity (but growing rapidly)
- Some specialized tools may require custom implementation
- WebGL export requires optimization for performance

### Neutral
- Learning Godot-specific patterns alongside C# knowledge
- Export pipeline differs from other engines

## Alternatives Considered

1. **Unity**: Popular but increasingly complex licensing, weaker web export story initially.
2. **Unreal Engine**: Overkill for a 2D/text-based game, heavy resource requirements.
3. **Custom Engine**: Prohibitive time investment; reinventing the wheel.
4. **XNA/MonoGame**: 
   - **Pros**: Native C# support, lightweight framework, active MonoGame community
   - **Cons**: 
     - XNA is legacy (unsupported since 2013)
     - MonoGame lacks mature web export (experimental WebGL)
     - Limited mobile support (Android/iOS secondary concern)
     - No visual editor; entirely code-based scene management
     - Smaller community compared to Godot
     - Text UI rendering would require custom shader implementation
     - Cross-platform deployment significantly more complex
   - **Decision**: Rejected. While MonoGame is viable, Godot provides superior multi-platform support, native editor, and stronger community resources.
5. **LibGDX**: Java-based, excellent for desktop but not ideal for web/mobile port targets.

## Related ADRs

- [ADR-002: Platform Strategy][adr-002]

---

**Owner**: Project Lead  
**Last Updated**: 2026-05-15
