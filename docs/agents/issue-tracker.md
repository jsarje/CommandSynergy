# Issue tracker: GitHub

Issues and specs for this repo live as GitHub issues. Use the `gh` CLI for all operations.

## Conventions

- Create an issue with `gh issue create --title "..." --body "..."`.
- Read an issue with `gh issue view <number> --json number,title,body,labels,comments`.
- List issues with `gh issue list --state open`; add label or state filters as needed.
- Apply or remove labels with `gh issue edit <number> --add-label "..."` or `--remove-label "..."`.
- Comment with `gh issue comment <number> --body "..."`.
- Close only after posting an explanation with `gh issue comment`, then run `gh issue close`.
- Infer the repository from `git remote -v`; before creating an issue, verify the target with `gh repo view --json nameWithOwner` and file only into this repository.

## Pull requests as a triage surface

**PRs as a request surface: no.** Set to `yes` only if this repo starts treating external PRs as feature requests.

## When a skill says “publish to the issue tracker”

Create a GitHub issue.

## When a skill says “fetch the relevant ticket”

Read the referenced issue with `gh issue view <number> --json number,title,body,labels,comments`.
