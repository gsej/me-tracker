# me-tracker

A personal weight tracking app with an Angular frontend and a .NET backend API.

## Architecture

| Component | Technology | Location |
|-----------|-----------|----------|
| Frontend | Angular (PWA) | `code/angular/` |
| Backend API | .NET 10 (ASP.NET Core) | `code/dotnet/Api/` |
| Database | SQLite | on a persistent mount on the server (`/opt/me-tracker-data`) |

The frontend is a static Angular app hosted on GitHub Pages. It communicates with a backend API running on a Proxmox LXC.

The backend runs in a Docker container on the LXC. It reads and writes a SQLite database file on a persistent host mount (`/opt/me-tracker-data/database`); the matching `.env` with secrets is delivered to the same location by Ansible.

## Deployment

### Frontend

Deployed via a GitHub Actions workflow (`.github/workflows/webui-prod.yml`), triggered with **workflow_dispatch** from the GitHub Actions UI.

The workflow builds the Angular app and deploys the static files to **GitHub Pages**. The production API URL is injected at build time via the `API_URL` environment variable into `settings.json` (which points to `localhost:5200` in source control for local development).

### Backend

Deployed to a Proxmox LXC via infrastructure-as-code:

- **Terraform** (`infra/`) builds the LXC.
- **Ansible** (`ansible/`) configures it — installs Docker, clones the repo, delivers
  the `.env`, and builds/starts the API from `docker-compose.yml`. See
  `ansible/README.md` for the full run steps.

`docker-compose.yml` builds the image from `code/dotnet/Api/Dockerfile` and
starts the container on port 5200, with the database volume and secrets at
`/opt/me-tracker-data` (provisioned by Terraform, populated by Ansible).

#### Verifying the running build

The health endpoint returns the git hash the image was built from:

```bash
curl https://<server>/api/healthz
# {"status":"Healthy","gitHash":"abc1234"}
```

Compare against `git rev-parse --short HEAD` to confirm the new image is serving.
