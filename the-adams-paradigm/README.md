# The Adams Paradigm

Personal developer brand and portfolio site for **Dean Adams** — "Turning Ideas Into Powerful Digital Experiences."

## Stack

- React 19 + TypeScript
- Vite
- Tailwind CSS v4
- lucide-react (icons)

## Getting started

```bash
npm install
npm run dev      # start the dev server
npm run build    # type-check and build for production
npm run preview  # preview the production build
npm run lint     # run oxlint
```

## Project structure

```
src/
  components/   reusable UI building blocks
  sections/     page sections composed in App.tsx
  data/         structured content (services, projects, technologies, etc.)
  hooks/        scroll reveal, mouse tracking, scroll spy, reduced motion
  lib/          small shared utilities
```

Content such as services, projects, technologies and process steps lives in
`src/data/*.ts` so it can be edited without touching component code.
