# Single-File Constraint

## Summary

All game code lives in one file: `index.html`. This is a deliberate constraint, not a limitation.

## Current State

The entire game — HTML structure, CSS styling, and JavaScript logic — is contained in `index.html` (~450 lines). Phaser 3 is the only external dependency, loaded via CDN.

## Design

### Why single-file?

- **Simplicity** — no build step, no dependency management, no configuration files
- **Portability** — the game works by opening one file in a browser
- **Learning** — forces understanding of how everything fits together without framework abstractions
- **Offline** — after the CDN script loads once, the game works offline (with PWA potential)

### How it works

- `<style>` tag contains all CSS (UI overlay, responsive layout, theming)
- `<script>` tag wraps all JS in an IIFE to avoid global scope pollution
- Phaser scenes are defined as classes inside the IIFE
- Game state is a plain object (`state`) at the top of the script
- Constants use UPPER_SNAKE_CASE at the top of the script

### When to break it

The single-file constraint should be revisited when:
- The file exceeds ~3000 lines and navigation becomes painful
- Multiple distinct systems need isolation for testability
- A build step becomes necessary for other reasons (asset pipeline, minification)
- Team collaboration requires clearer file boundaries

Even then, prefer splitting into a small number of files (2–4) rather than a full module system.

## Open Questions

- At what line count does the single-file approach become a net negative?
- Should CSS be the first thing to extract if splitting happens?
