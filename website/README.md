# Loggle Website

A React + TypeScript landing page for Loggle built with Create React App. Content highlights insights gathered from **Competitor Landscape in Log Management.pdf** and keeps the layout intentionally simple.

## Prerequisites

- Node.js 20.x (ships with npm 10)
- Git (for cloning) and a terminal

Check your versions:

```powershell
node --version
npm --version
```

If you need Node.js, install it from [nodejs.org](https://nodejs.org/) or via your preferred version manager.

## Install dependencies

From the repository root:

```powershell
cd website
npm install
```

This installs React, CRA tooling (`react-scripts`), TypeScript, and the support libraries used on the page.

## Start the development server

```powershell
npm start
```

CRA serves the site at `http://localhost:3000`. Edits under `website/src/` reload automatically.

### Project layout

- `src/App.tsx` – page structure and content
- `src/App.css` – component styling and responsive rules
- `src/data/features.ts` – feature, outcome, and integration copy blocks sourced from the competitor PDF
- `src/data/docs.ts` – README highlights that power the Docs tiles
- `src/components/MermaidDiagram.tsx` – reusable wrapper around Mermaid for the architecture flowchart
- `public/index.html` – HTML shell with fonts/meta tags

## Format and build

```powershell
npm run build  # Bundles into the CRA "build/" directory
```

The build step outputs a production bundle into `website/build/`.

## Docker image

The Dockerfile mirrors the sqloom portal image: it builds with Node 20 Alpine and serves the CRA output using [`serve`](https://www.npmjs.com/package/serve).

Build and run from the repository root:

```powershell
docker build -t loggle-website:latest -f website/Dockerfile website

docker run --rm -p 3000:3000 loggle-website:latest
```

Visit `http://localhost:3000` to view the site running inside the container.

## Lockfile refresh (important for Docker builds)

When `package.json` changes, regenerate the lockfile before building in Docker:

```powershell
cd website
Remove-Item -Recurse -Force node_modules
Remove-Item -Force package-lock.json
npm install
```

This keeps `npm ci` reproducible across environments (including Alpine-based containers).

## Inspiration & sources

- Messaging is inspired by `website_template/Competitor Landscape in Log Management.pdf` and hyperlinks back to the root `README.md` for authoritative documentation.
