# me-tracker deployment (Ansible)

Terraform (`../infra`) builds the LXC; this playbook configures it: installs
Docker, clones the repo, and builds/starts the API from
`docker-compose.proxmox.yml`.

## Structure

```
ansible/
├── ansible.cfg              # connects as root, uses inventory.ini
├── inventory.ini           # the one container (set its IP here)
├── requirements.yml        # community.docker collection
├── site.yml                # top-level playbook → runs the metracker role
└── roles/metracker/
    ├── defaults/main.yml   # repo URL, paths, .env source
    └── tasks/main.yml      # install Docker, clone, deliver .env, build + up
```

## One-time setup

Arch's `ansible` package bundles `community.docker`, so no galaxy step:

```sh
sudo pacman -Syu ansible
```

## Run it

```sh
cd ansible
# point inventory.ini at the container IP (from `terraform output`)

ansible metracker -m ping            # connectivity check
ansible-playbook site.yml
```

The playbook:
1. installs Docker,
2. clones the public `me-tracker` repo to `/opt/me-tracker`,
3. copies `.env` from `../../me-tracker-private/.env` to
   `/opt/me-tracker-data/.env` (the DB is **not** copied — the app creates an
   empty one; restore real data via the API out-of-band),
4. builds and starts the API, then waits for `/api/healthz`.

API (phase 1): `http://<container_ip>:5200`.

## Data & restore

The SQLite DB lives on a dedicated Terraform mount at
`/opt/me-tracker-data/database`, so it survives container rebuilds. After first
deploy it's empty — restore a recent backup via the API restore endpoint.

## Not yet done (phase 2)

Tailscale Funnel to expose the API publicly — see the repo `TODO` / Frigate's
`SECURITY.md` for the exposure approach.
