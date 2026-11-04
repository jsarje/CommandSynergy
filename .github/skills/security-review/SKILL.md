---
name: security-review
description: Reviews changes to EDHREC URL construction, external JSON handling, and unauthenticated HTTP endpoints in CommandSynergy.
---

# Security review

## Trust boundaries in this repository

- HTTP request bodies enter through endpoint mappings in `src/CommandSynergy/Endpoints/`. The endpoints validate request inputs and return validation problems before calling application services.
- Commander names can contain user input. `EdhrecClient.BuildCommanderSlug` filters the name, and `SlugAllowlist` checks the resulting path segment before the client makes a request.
- `EdhrecOptions.BaseUrl` changes the first EDHREC destination. Treat this value as trusted deployment configuration, not request data. `EdhrecClient` also tries the canonical `https://json.edhrec.com/` address.
- EDHREC responses and cached values are deserialized in `src/CommandSynergy.Infrastructure/Edhrec/EdhrecClient.cs`.

## Before merging a change to EDHREC requests

- Keep commander names out of URL construction except through `BuildCommanderSlug`.
- Keep `EdhrecOptions.BaseUrl` sourced from configuration, not from an HTTP request.
- Keep timeout, HTTP, and malformed JSON failures logged and represented by empty theme insights.
- Run `dotnet test tests/CommandSynergy.Infrastructure.Tests/CommandSynergy.Infrastructure.Tests.csproj --filter FullyQualifiedName~EdhrecClientTests`.

## Before merging an endpoint change

- Add or update request-validation tests under `tests/CommandSynergy.WebUI.Tests/Endpoints/`.
- Keep invalid requests on the existing validation-problem response path.
- Do not deploy the unauthenticated app to a public network without adding an access boundary.
