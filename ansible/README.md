# me-tracker deployment (Ansible)

Terraform (`../infra`) builds the LXC; this playbook configures it: installs
Docker, clones the repo, and builds/starts the API and the frontend from
`docker-compose.yml`.

## Structure

```
ansible/
├── ansible.cfg              # connects as root; no default inventory (pass -i)
├── m900-inventory.ini       # M900 container (.221)
├── requirements.yml        # community.docker collection
├── secrets/                # control-machine secrets (gitignored) + sample templates
├── site.yml                # top-level playbook → runs the metracker role
└── roles/metracker/
    ├── defaults/main.yml   # repo URL, paths, .env secret source
    └── tasks/
        └── main.yml        # install Docker, clone, deliver .env, build + up
```

## One-time setup

Arch's `ansible` package bundles `community.docker`, so no galaxy step:

```sh
sudo pacman -Syu ansible
```

## Run it

The deployment target is M900, with its committed inventory file
(`m900-inventory.ini`) — the non-secret parallel to the per-env `tfvars`. No
inventory is auto-loaded, so pass it with `-i`. `../deploy.sh` runs the playbook
below for you.

```sh
cd ansible
ansible -i m900-inventory.ini metracker -m ping     # connectivity check
ansible-playbook -i m900-inventory.ini site.yml
```

The playbook:
1. installs Docker,
2. clones the public `me-tracker` repo to `/opt/me-tracker` (it builds from the
   box's clone, so push to `origin/master` first),
3. copies `.env` from `secrets/.env` to `/opt/me-tracker-data/.env` (the DB is
   **not** copied — the app creates an empty one; restore real data via the API
   out-of-band),
4. builds and starts the API and the frontend, then waits for `/api/healthz`
   and the frontend to respond.

On the LAN:
- API: `http://<container_ip>:5200`
- Frontend: `http://<container_ip>:5201` (nginx serving the built Angular app)

## Public exposure

Nothing on this box is exposed to the internet directly. The reverse proxy in
the `home-proxy` repo (LXC `.227`) serves `https://me-tracker.gsej.co.uk`: it
sends `/api/*` to port 5200 and everything else to port 5201, so the frontend
and the API share one origin. The frontend is built with an empty API URL, so it
calls `/api/...` on whichever site served it.

## Data & restore

The SQLite DB lives on a dedicated Terraform mount at
`/opt/me-tracker-data/database`, so it survives Docker container rebuilds, but not destroying or replacing the LXC. After first
deploy it's empty — restore a recent backup via the API restore endpoint.

## Secrets

Deploy secrets live in `ansible/secrets/` on the control machine. The real files
are **gitignored** (never committed to this public repo); the sample templates
and the **public** key are tracked. Restore/copy each from your password manager:

- `env.sample` → `secrets/.env` — API keys (`ApiKeys=...`), delivered to the box
  as `/opt/me-tracker-data/.env`.
- `secrets/deploy_key` (+ committed `secrets/deploy_key.pub`) — the SSH deploy
  key. Terraform injects the public key into the container; Ansible connects with
  the private key. Restore the private key from your password manager and
  `chmod 600` it.

The `.env` source path is `metracker_env_src` in `defaults/main.yml`; the key is
referenced by `<env>-inventory.ini` (public) and `infra/*-terraform.tfvars`
(public). The playbook checks `.env` exists up front and fails with a clear
message if it's missing.
