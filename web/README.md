# AI Agents From Scratch — Companion Website

This is a static Next.js website that renders the C# tutorial content from the parent repository.

## Getting Started

```bash
cd web
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Build & serve locally

```bash
npm run build
npm run serve
```

The static export is written to `out/`.

## Content pipeline

`npm run extract` reads the chapters under `src/ChapterNN/` and the companion docs under `examples/NN_name/`, then generates:

- `src/data/generated/chapters.json`
- `src/data/generated/docs.json`

These files are regenerated automatically by `npm run dev` and `npm run build`.
