# Parley Web

Next.js 16 App Router application.

## Commands

- `pnpm install` - install dependencies
- `pnpm dev` - start the development server
- `pnpm run check` - run typecheck, lint, formatting check
- `pnpm run test` - run unit tests
- `pnpm run build` - create a production build
- `pnpm run api:generate` - regenerate API types from `API_BASE_URL/openapi/v1.json`

Copy `.env.example` to `.env.local` and set `API_BASE_URL` when the API runs on a different port.

## Structure

- `src/app` - Next.js routes; route files only compose feature components
- `src/features/<area>` - `components`, `hooks`, `schemas` per feature (auth, members, teams, organization, profile, audit, session)
- `src/components` - shared UI: `ui` primitives, `layout`, `forms`, app providers
- `src/lib/api` - API client, generated types, browser BFF client, API hooks
- `src/lib/bff` - server-only BFF code: request guard, proxy allow-list, backend fetch, auth handlers
- `src/lib/{session,errors,validation}` - shared session, error and field-validation helpers
- `src/constants`, `src/i18n` - constants and localization setup
- Tests sit next to the code they cover
