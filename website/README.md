# Loggle Website

Loggle's landing page is built with React and TypeScript using Create React App.

## Requirements

- Node.js 20.x (includes npm 10)
- Git

Verify your local versions:

```powershell
node --version
npm --version
```

Install Node.js from [nodejs.org](https://nodejs.org/) or your preferred version manager if needed.

## Installation

From the project root:

```powershell
cd website
npm install
```

This installs Create React App tooling, TypeScript, and the dependencies used by the site.

## Development server

```powershell
npm start
```

The site runs at `http://localhost:3000` with automatic reloads for changes under `website/src/`.

## Build

```powershell
npm run build
```

The production bundle is emitted to `website/build/`.

## Docker

```powershell
docker build -t loggle-website:latest -f website/Dockerfile website
docker run --rm -p 3000:3000 loggle-website:latest
```

Browse to `http://localhost:3000` to confirm the containerized build.

## Project structure

- `src/App.tsx` – page layout and content
- `src/App.css` – styling and responsive rules
- `src/data/features.ts` – feature copy displayed across the page
- `src/data/docs.ts` – documentation links for the Docs section
- `src/components/MermaidDiagram.tsx` – wrapper component for the architecture diagram
- `public/index.html` – base HTML shell

## Dependency updates

If `package.json` changes, refresh the lockfile before producing Docker images:

```powershell
cd website
Remove-Item -Recurse -Force node_modules
Remove-Item -Force package-lock.json
npm install
```

This keeps `npm ci` reproducible across environments, including Alpine-based containers.
