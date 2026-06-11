# Deployment (backend)

This repository is the **backend** (ASP.NET Core Web API). It is containerized and
published as a single image to **GitHub Container Registry (GHCR)** by the `deploy`
workflow, then deployed to **Render**:

- `ghcr.io/<owner>/homeinventory-backend`

The frontend lives in a **separate repository** and deploys to **Vercel** (static SPA),
so it is not part of this repo's containerization, compose file or workflows. The only
backend-side coupling is CORS (see below).

## Local development

```bash
docker compose up --build
```

This starts (backend only):

- **postgres** on `localhost:5432` (db `homeinventory`, user `postgres`, dev password)
- **backend** on `localhost:8080` (migrations applied on startup)

Run the frontend separately (e.g. `yarn dev` in its own repo) and point it at
`http://localhost:8080`. All values in `docker-compose.yml` are local development
defaults only; no production secrets belong in the repo or in the image.

## Backend environment variables (production / Render)

Set these as environment variables on the Render service. Never commit them.

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__Default` | Postgres connection string (e.g. the Neon database). |
| `Jwt__SigningKey` | Symmetric signing key for JWTs (>= 32 chars). |
| `Storage__S3__BucketName` | S3 bucket for stored files. |
| `Storage__S3__Region` | S3 region. |
| `Storage__S3__AccessKeyId` | S3 access key id. |
| `Storage__S3__SecretAccessKey` | S3 secret access key. |
| `Cors__AllowedOrigins__0` | Production frontend origin allowed by CORS (add `__1`, `__2`, ... for more). |
| `RUN_MIGRATIONS_ON_STARTUP` | `true` to apply EF Core migrations on startup. |
| `PORT` | Port to listen on. Render sets this automatically; defaults to `8080` if unset. |

Notes:

- The double underscore (`__`) maps to nested configuration keys (`Jwt:SigningKey`, etc.).
- No secrets are baked into the image; they are read from the environment at runtime.

### CORS

The backend only allows the origins listed in `Cors__AllowedOrigins__*`. In production you
**must** add the frontend's Vercel domain (for example `https://your-app.vercel.app`, plus
any custom domain), otherwise the browser will block API calls. The frontend also needs
**HTTPS** for its in-app QR scanner (camera access requires a secure context) — Vercel
provides this automatically.

### Database migrations

`RUN_MIGRATIONS_ON_STARTUP=true` applies pending migrations when the app starts. This is
convenient for single-instance hosting (Render). For multi-instance deployments, leave it
unset and run migrations as a separate one-off step (for example `dotnet ef database
update` or a dedicated job) to avoid concurrent migration attempts.

## CI/CD

- **CI** (`.github/workflows/ci.yml`): restore, build and `dotnet test` on every PR and
  push to `main`.
- **CD** (`.github/workflows/deploy.yml`): on push to `main`, build the image (context
  `backend/`) and push `homeinventory-backend:latest` and `:<sha>` to GHCR using the
  built-in `GITHUB_TOKEN` (`permissions: packages: write` — no extra registry credentials).

### Deploying to Render

Two options:

1. **Deploy hook** — set the repository *Secret* `RENDER_DEPLOY_HOOK_URL`. The `deploy`
   workflow calls it after pushing the image to trigger a Render deploy.
2. **Auto-deploy** — connect this repository (or the GHCR image) directly in the Render
   dashboard so Render deploys on every push. In that case the optional
   `trigger-render-deploy` job can be removed.
