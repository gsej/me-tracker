# me-tracker

A personal weight tracking app with an Angular frontend and a .NET backend API.

## Architecture

| Component | Technology | Location |
|-----------|-----------|----------|
| Frontend | Angular (PWA) | `code/angular/` |
| Backend API | .NET 10 (ASP.NET Core) | `code/dotnet/Api/` |
| Database | SQLite | on a persistent mount on the server (`/opt/me-tracker-data`) |

Both run on a Proxmox LXC, each in its own Docker container: the API, and nginx serving the built Angular app. A reverse proxy (the `home-proxy` repo) serves them together at `https://me-tracker.gsej.co.uk`, sending `/api/*` to the API and everything else to the frontend.

The API reads and writes a SQLite database file on a persistent host mount (`/opt/me-tracker-data/database`); the matching `.env` with secrets is delivered to the same location by Ansible.

## Deployment

Both parts are deployed to a Proxmox LXC via infrastructure-as-code:

- **Terraform** (`infra/`) builds the LXC.
- **Ansible** (`ansible/`) configures it — installs Docker, clones the repo, delivers
  the `.env`, and builds/starts the API and the frontend from `docker-compose.yml`.
  See `ansible/README.md` for the full run steps.

To deploy, push to `master` (the box builds from its own clone), then run:

```bash
./deploy.sh
```

`docker-compose.yml` has two services:

- **`api`** builds from `code/dotnet/Api/Dockerfile` and runs on port 5200, with
  the database volume and secrets at `/opt/me-tracker-data` (provisioned by
  Terraform, populated by Ansible).
- **`web`** builds the Angular app with `code/angular/Dockerfile` and serves it
  with nginx on port 5201. The build bakes in the commit hash and an empty API
  URL (`ng build --define API_URL=''`), so the app calls `/api/...` on whichever
  site served it. Without these defines, as in local development, the app uses
  `http://localhost:5200` and shows `ui: local`.

The proxy in `home-proxy` is what makes them public; nothing on this box is
exposed to the internet directly.

### Verifying the running build

The health endpoint returns the git hash the image was built from:

```bash
curl https://me-tracker.gsej.co.uk/api/healthz
# {"status":"Healthy","gitHash":"abc1234"}
```

Compare against `git rev-parse --short HEAD` to confirm the new image is serving.
