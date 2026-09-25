# me-tracker

A personal tracking app with an Angular frontend and a .NET backend API.

## Architecture

| Component | Technology | Location |
|-----------|-----------|----------|
| Frontend | Angular (PWA) | `code/angular/` |
| Backend API | .NET 8 (ASP.NET Core) | `code/dotnet/Api/` |
| Database | SQLite | mounted from `../me-tracker-private/database/` |

The frontend is a static Angular app hosted on GitHub Pages. It communicates with the backend API over HTTPS via a Tailscale Funnel URL (`frigate.tailbdb963.ts.net`), which publicly exposes the API running on the home server.

The backend runs in a Docker container on a home server. It reads and writes a SQLite database file that lives outside the repo in a private directory (`../me-tracker-private/`), which also holds secrets via a `.env` file.

## Deployment

### Frontend

The frontend is deployed manually via a GitHub Actions workflow (`.github/workflows/webui-prod.yml`), triggered using **workflow_dispatch** (run it by hand from the GitHub Actions UI).

The workflow:
1. Checks out the repo and installs Node 20 dependencies.
2. Builds the Angular app with `npm run build -- --base-href /me-tracker/`.
3. Populates `settings.json` from `settings.template.json` using `envsubst`, injecting the API URL and current git hash.
4. Deploys the built static files to **GitHub Pages**.

The API URL is a runtime configuration — the Angular app fetches `settings.json` on startup and reads `apiUrl` from it (`SettingsHttpService`). The source-controlled `settings.json` points to `localhost:5200` for local development; the production value is injected during the build via the workflow's `API_URL` environment variable.

#### PWA updates on phones

No manifest changes are needed when deploying an update. Every build produces an `ngsw.json` file containing hashes of all cached assets. When the app is opened on a phone, the Angular service worker fetches `ngsw.json` from the network and compares hashes against its cache. If anything has changed it downloads the new version in the background. The update is applied on the **next** app restart, not the current one — so users will see the new version the second time they open the app after a deployment.

### Backend

The backend is deployed manually on the home server:

```bash
git pull
docker compose up -d --build
```

`docker-compose.yml` builds the image from `code/dotnet/Api/Dockerfile` and starts the container on port 5200, with the database volume and secrets mounted from `../me-tracker-private/`.

The API is exposed publicly via **Tailscale Funnel**, which provides an HTTPS endpoint accessible from the internet without opening firewall ports.
