---
name: verify
description: Drive the CommandSynergy Blazor Commander deckbuilding workspace in a live browser when you need end-to-end proof of card search, deck editing, library persistence, import/export, analysis, or playtesting.
---

# Verify CommandSynergy

Use this skill to exercise CommandSynergy through its Blazor workspace, not through component setters or test-only endpoints. The JSON API is a secondary surface. The app has no login or database.

## Launch

Run `dotnet restore src/CommandSynergy.slnx` from the repository root on a fresh checkout. Start the app in a dedicated background PowerShell session with an empty card metadata directory and a free loopback port:

```powershell
$runId = [guid]::NewGuid().ToString('N')
$runRoot = Join-Path (Get-Location) "artifacts\verification\$runId"
$evidenceDir = Join-Path $runRoot 'evidence'
$scratchDir = Join-Path $runRoot 'scratch'
$dataDir = Join-Path $scratchDir 'card-metadata'
New-Item -ItemType Directory -Force -Path $evidenceDir, $dataDir | Out-Null

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
$listener.Start()
$port = $listener.LocalEndpoint.Port
$listener.Stop()
$baseUrl = "http://127.0.0.1:$port"

[pscustomobject]@{
    RunId = $runId
    Port = $port
    BaseUrl = $baseUrl
    RepositoryRoot = (Get-Location).Path
    ScratchDirectory = $scratchDir
    DataDirectory = $dataDir
    EvidenceDirectory = $evidenceDir
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidenceDir 'run.json')

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:CardMetadata__SnapshotDirectory = $dataDir
dotnet run --project src/CommandSynergy/CommandSynergy.csproj --no-launch-profile --urls $baseUrl
```

Keep the PowerShell session running and record its session ID, the run ID, and the port. The app must log `Now listening on: http://127.0.0.1:<port>`. Open a new Browser canvas instance for the printed URL. It is ready only when `read_page` shows `Synergy Sphere`, `Deck signals`, `Find cards`, and `Command zone and mainboard`, with no reconnect or resume message.

Development mode serves the app's static web assets. Without it, the workspace can return HTML while failing to load Blazor and MudBlazor assets. The empty metadata directory is valid. The app has no migrations or seed command. It logs that `cards.parquet` is missing and uses its read-only Scryfall fallback for card search. Do not run ingestion for a normal verification run.

## Doctor

Run this read-only check as soon as the app starts and before driving it. Set `$evidenceDir` to the path recorded in `run.json`.

```powershell
$run = Get-Content -Raw -LiteralPath (Join-Path $evidenceDir 'run.json') | ConvertFrom-Json
$listener = @(Get-NetTCPConnection -LocalPort ([int]$run.Port) -State Listen -ErrorAction SilentlyContinue)
if ($listener.Count -ne 1) { throw "Expected one listener on port $($run.Port)" }

$process = Get-Process -Id $listener[0].OwningProcess -ErrorAction Stop
$expectedPath = Join-Path $run.RepositoryRoot 'src\CommandSynergy\bin\Debug\net10.0\CommandSynergy.exe'
if ($process.ProcessName -ne 'CommandSynergy' -or $process.Path -ne $expectedPath) {
    throw "Port $($run.Port) is not owned by this worktree's CommandSynergy build"
}

$response = Invoke-WebRequest -Uri $run.BaseUrl -TimeoutSec 10
if ($response.StatusCode -ne 200 -or
    -not $response.Content.Contains('Commander - Synergy Sphere') -or
    -not $response.Content.Contains('_framework/blazor.web.js')) {
    throw 'The app response is not the expected Blazor workspace'
}
```

Then call Browser canvas `read_page`. Confirm the workspace is interactive and its title and controls match this checkout. `dotnet --version` must report .NET 10. The app has no build-revision endpoint, so the process path is the available check that the listener belongs to the current worktree. There is no authentication to validate.

If the check cannot confirm that this worktree owns the listener, do not drive it or stop another process. Pick another free port, launch a new run, and repeat the Doctor check.

## Drive

Use the Browser canvas with the unique URL and a unique instance ID for this run. Call `read_page` before each interaction, then use its current element references with `click_element` and `type_in_page`. Read the page again after each action and wait for the real result instead of a fixed delay.

Use the mapped recipes in [the feature index](./features/README.md). The workspace's stable user-facing handles include the `Search commanders, staples, or role players` input, the `Search` button, `Open Deck Library`, and `Open Playtest`. The app also exposes `data-testid` values such as `workspace-briefing`, `search-results`, `workspace-utility-menu`, `playtest-overlay`, and the deck signal metric triggers.

MudBlazor renders the `Import a deck` and `Export` accordion headers as visible `div` elements without button roles. If `read_page` does not provide a clickable reference, use Browser canvas `evaluate_javascript` only to click the visible header by its text:

```javascript
const header = Array.from(document.querySelectorAll('.mud-expand-panel-header'))
  .find(element => element.textContent.trim() === 'Import a deck');
if (!header) throw new Error('Import a deck header is missing');
header.click();
return header.textContent.trim();
```

This clicks the actual visible control. Do not call component methods, write application state from JavaScript, or use test endpoints as a substitute for a user journey. The only JavaScript state change allowed is cleanup: remove `command-synergy.imported-decks` from this run's fresh origin after you delete the test deck through Deck Library. Card search uses the production Scryfall client when the snapshot has no match. Leave that boundary real for end-to-end proof.

## Evidence

Store proof under `artifacts\verification\<run-id>\evidence\`. Keep `run.json` and a `proof.md` with the run ID, URL, feature ID, each user action, the observed result, and any unmet precondition. Save the before and after `read_page` snapshots beside it. Use Browser canvas `screenshot_page` to capture the action state and the resulting state. The canvas screenshot remains in the session transcript; copy it into the evidence directory if the browser provides a file export, and confirm the file exists before naming it in `proof.md`.

Exercise the real user path and capture both the action and the result. For mutations, verify the visible state and the durable side effect. Saved decks use the browser localStorage key `command-synergy.imported-decks`; check that the value is present after saving and after reload, but do not dump its compressed contents into evidence. The app does not write Parquet during card search or deck analysis. Record relevant Scryfall or EDHREC requests from the app log when they affect the result. Do not replace these calls with mocks.

## Cleanup

Delete only test data created by this run. First delete any saved test deck through Deck Library's confirmation dialog. Reload the page and verify that the library is empty. The app may keep an empty serialized library value, so remove only `command-synergy.imported-decks` from this run's unique browser origin and verify that key is absent.

Read `run.json` and confirm that the listening process path matches the expected `CommandSynergy.exe` under `RepositoryRoot`. Stop that exact process ID or stop the dedicated PowerShell command session that started it. Never kill a process by name. Verify that the process is gone and the recorded port has no listener. Remove only the path recorded in `ScratchDirectory`. Keep `evidence`, including `run.json`, proof snapshots, logs, and screenshots, after teardown.

## Helpers

This skill ships no helper scripts. Use the launch and doctor commands above, the Browser canvas actions, and the feature recipes. Keep each app process, browser instance, port, and data directory private to one run. If another run may share any of them, refuse to drive until it has a separate port, data directory, and Browser canvas instance.
