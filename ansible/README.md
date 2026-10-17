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
    ├── defaults/main.yml   # repo URL, paths, .env + Tailscale secret sources
    ├── handlers/main.yml   # restart tailscaled
    └── tasks/
        ├── main.yml        # install Docker, clone, deliver .env, build + up
        └── tailscale.yml   # install Tailscale, authenticate, expose via Funnel
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
4. builds and starts the API, then waits for `/api/healthz`,
5. installs Tailscale and exposes the API over public HTTPS via Funnel
   (phase 2 — see below).

- LAN (phase 1): `http://<container_ip>:5200`.
- Public (phase 2): `https://<hostname>.<tailnet>.ts.net` — the exact URL is
  printed by the final playbook task (`tailscale funnel status`).

## Data & restore

The SQLite DB lives on a dedicated Terraform mount at
`/opt/me-tracker-data/database`, so it survives container rebuilds. After first
deploy it's empty — restore a recent backup via the API restore endpoint.

## Public exposure (phase 2 — Tailscale Funnel)

The API is exposed to the internet over HTTPS via Tailscale Funnel, configured
by `tasks/tailscale.yml`. The GitHub Pages frontend calls this public URL.

Because the LXC is **unprivileged** (no `/dev/net/tun`), `tailscaled` runs in
**userspace-networking** mode — Funnel only proxies a local TCP port, which that
mode supports without any device passthrough. Nothing in Terraform changes.

### One-time Tailscale admin-console setup

None of this lives in source control (it's tailnet state), so redo it on a fresh
tailnet. In the [admin console](https://login.tailscale.com/admin):

1. **Policy** (Access Controls):
   - `tagOwners`: `"tag:metracker": ["autogroup:admin"]`.
   - `nodeAttrs`: grant the `funnel` attribute to `tag:metracker`. The default
     funnel entry targets `autogroup:member`, which does **not** include tagged
     devices — so add `tag:metracker` explicitly.
2. **HTTPS** (DNS settings): enable MagicDNS + HTTPS certificates (Funnel needs it).
3. **OAuth client** (Settings → Trust credentials → Generate credential → OAuth):
   scope `auth_keys` → **Write** only (not "All"), tag `tag:metracker`. Copy the
   one-time secret.

### Secret

Put the OAuth client secret in the sibling private repo (never committed), as a
single line, matching `metracker_ts_oauth_src` in `defaults/main.yml`:

```sh
# me-tracker-private/tailscale-oauth.env
TS_OAUTH_SECRET=tskey-client-xxxxxxxx
```

### Notes

- Re-runs are idempotent: `tailscale up` is skipped once the node is `Running`,
  and Funnel is only (re)set if port 5200 isn't already published.
- Unlike the app code, Tailscale config takes effect **without** pushing to
  `origin/master` — it runs from this control-machine checkout, not the box's
  clone. (The push rule only applies to app code that Ansible builds on the box.)
- `terraform destroy`/recreate leaves a stale node in the tailnet; delete it in
  the console or re-run the playbook to re-adopt the hostname.
