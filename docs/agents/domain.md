# Domain Docs

How engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`GLOSSARY.md`** at the repo root, or
- **`GLOSSARY-MAP.md`** at the repo root if it exists: it points at one `GLOSSARY.md` per context. Read each one relevant to the topic.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in. In multi-context repos, also check `src/<context>/docs/adr/` for context-scoped decisions.

If any of these files don't exist, proceed silently. Don't flag their absence or suggest creating them upfront; `/domain-modeling` creates them lazily when terms or decisions are resolved.

## File structure

Single-context repo (the default):

```
/
├── GLOSSARY.md
├── docs/adr/
└── src/
```

Multi-context repo (only if a root `GLOSSARY-MAP.md` exists):

```
/
├── GLOSSARY-MAP.md
├── docs/adr/                  # system-wide decisions
└── src/
    └── <context>/
        ├── GLOSSARY.md
        └── docs/adr/          # context-specific decisions
```

## Use the glossary's vocabulary

When output names a domain concept (for example, in an issue title, refactor proposal, hypothesis, or test name), use the term defined in `GLOSSARY.md`. Don't drift to synonyms the glossary explicitly avoids. If a needed concept isn't defined, reconsider whether the project's language is being invented or note the gap for `/domain-modeling`.

## Flag ADR conflicts

If output contradicts an existing ADR, surface that explicitly rather than silently overriding it.
