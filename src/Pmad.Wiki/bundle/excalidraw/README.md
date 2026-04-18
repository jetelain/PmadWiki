# Excalidraw Bundle

This folder contains the build tooling to produce a self-hosted ESM bundle of
[Excalidraw](https://excalidraw.com/) and its React dependencies, so that the
library can be shipped with `Pmad.Wiki` without any CDN dependency at runtime.

## Output

The build writes the following files into `src/Pmad.Wiki/wwwroot/lib/excalidraw/`:

| File | Description |
|---|---|
| `excalidraw-bundle.js` | Single minified ESM file — React, ReactDOM and Excalidraw bundled together |
| `excalidraw.css` | Excalidraw stylesheet |
| `*` (other files) | Static assets (stylesheet, fonts, locale files, …) loaded at runtime via `EXCALIDRAW_ASSET_PATH` |

React and Excalidraw are bundled into the **same** module so that only one React
instance exists on the page — a requirement for React hooks to work correctly.
Consumers import everything from the single bundle file:

```js
import { ExcalidrawLib, React, ReactDOM, createRoot } from '/lib/excalidraw/excalidraw-bundle.js';
```

## Prerequisites

- [Node.js](https://nodejs.org/) 18 or later
- npm 8 or later (bundled with Node.js)

## Build instructions

### Windows (PowerShell)

```powershell
cd src\Pmad.Wiki\bundle\excalidraw
npm ci
npm run build
```

### Linux / macOS / WSL2

```bash
cd src/Pmad.Wiki/bundle/excalidraw
./build.sh
```

`build.sh` runs `npm install` followed by `npm run build`.

> **CI note:** the GitHub Actions workflows run `npm ci && node build.mjs`
> before `dotnet build` so that the generated `wwwroot` assets are present
> when MSBuild packages the library.

## Upgrading Excalidraw or React

1. Edit `package.json` — bump the version(s) in `dependencies`.
2. Run `npm install` to regenerate `package-lock.json`.
3. Run `npm audit` to check for new vulnerabilities.
4. Run `npm run build` and smoke-test the demo project.
5. Commit both `package.json` and `package-lock.json`.

## Security overrides

Because `@excalidraw/excalidraw` 0.18.0 is the latest available release and
pins several transitive dependencies that have known vulnerabilities, the
`overrides` field in `package.json` forces patched versions that do not
introduce breaking changes for the bundle:

| Package | Forced version | Vulnerability |
|---|---|---|
| `dompurify` | `^3.3.4` | Multiple XSS CVEs in `<=3.3.3` ([GHSA-vhxf-7vqr-mrjg](https://github.com/advisories/GHSA-vhxf-7vqr-mrjg) and others) — used by `mermaid` |
| `mermaid` | `^10.9.4` | XSS in sequence diagram labels in `10.9.0-rc.1 – 10.9.3` ([GHSA-7rqq-prvp-x9jh](https://github.com/advisories/GHSA-7rqq-prvp-x9jh)) — used by `@excalidraw/mermaid-to-excalidraw` |
| `nanoid` | `^3.3.8` | Predictable ID generation in `<3.3.8` ([GHSA-mwcw-c2x4-8c55](https://github.com/advisories/GHSA-mwcw-c2x4-8c55)) — used directly by Excalidraw |
| `@excalidraw/mermaid-to-excalidraw` → `nanoid` | `^5.0.9` | Same advisory, affects the `4.0.2` copy nested inside the mermaid converter |

> **Risk note:** the nested `nanoid` override crosses a major version boundary
> (4 → 5). If `@excalidraw/mermaid-to-excalidraw` breaks at runtime the only
> side-effect is that Mermaid diagram import inside Excalidraw stops working;
> the core drawing canvas is unaffected.

When a new version of `@excalidraw/excalidraw` is released, re-run
`npm audit` to verify whether these overrides are still needed and remove any
that are no longer required.
