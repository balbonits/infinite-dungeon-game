# ADR-003: Spec-Driven Development

**Status:** Accepted
**Date:** 2026-04-08

**Context:** This project is built entirely by AI assistants, directed by a product owner who is not a developer. The AI is the entire dev team -- there is no human programmer reviewing code or catching design misunderstandings. Without clear, unambiguous specifications, AI assistants hallucinate requirements, add unspecified features, and make assumptions that compound into bugs and wasted effort.

**Decision:** Every system must be fully specified in a markdown document under `docs/` before any code is written. 26 spec documents were written and locked before the first line of implementation code. Specs are the source of truth -- if code and docs disagree, one needs updating.

Workflow:
- **Specs before code** -- no exceptions
- **Tests before implementation** -- test cases are derived from specs, then code is written to pass the tests
- **One task = one commit** -- focused changes that map to spec sections
- **Three-tier boundary system** -- "always do" (read specs, run tests), "ask first" (new files, dependency changes), "never do" (unspecified features, assumptions)
- **Docs are the source of truth** -- the 26 spec documents in `docs/` define what the game does

**Consequences:**
- The product owner can review and approve game design by reading plain English documents, not code
- AI assistants have unambiguous instructions, reducing hallucination and scope creep
- Changing a game system starts with updating the spec document, then updating the code to match
- The upfront cost of writing 26 specs before any code delayed implementation start but dramatically reduced ambiguity during implementation
- All formulas, data models, and interaction rules are documented and searchable, making it easy to onboard new AI sessions
