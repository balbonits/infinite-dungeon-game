# AGENTS.md – Guidelines for AI Coding Assistants

This file is a reference for any AI coding tool helping with **A Dungeon in the Middle of Nowhere** (repo: dungeon-web-game).

The game is a **single-file, offline browser-based action RPG** built with Phaser 3 via CDN.  
Everything must stay in **one HTML file** (`index.html`) for as long as possible — no separate JS/CSS files, no npm/Vite/Parcel, no build step.

## Core Project Rules (must follow)

1. **Single-file constraint**  
   - All code (JS, CSS, HTML) lives inside `index.html`  
   - Use `<script>` and `<style>` tags  
   - Phaser loaded via CDN:  
     ```html
     <script src="https://cdn.jsdelivr.net/npm/phaser@3.90.0/dist/phaser.min.js"></script>