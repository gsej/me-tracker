# me-tracker deployment (Ansible)

Terraform (`../infra`) builds the LXC; this playbook configures it: installs
Docker, clones the repo, and builds/starts the API from
`docker-compose.yml`.

## Structure

```
ansible/
├── ansible.cfg              # connects as root, uses inventory.ini
├── inventory.ini           # the one container (set its IP + ts hostname here)
├── requirements.yml        # community.docker collection
├── secrets/                # control-machine secrets (gitignored) + sample templates
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
3. copies `.env` from `secrets/.env` to `/opt/me-tracker-data/.env` (the DB is
   **not** copied — the app creates an empty one; restore real data via the API
   out-of-band),
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

### Secrets

Deploy secrets live in `ansible/secrets/` on the control machine. The real files
are **gitignored** (never committed to this public repo); the sample templates
and the **public** key are tracked. Restore/copy each from your password manager:

- `env.sample` → `secrets/.env` — API keys (`ApiKeys=...`), delivered to the box
  as `/opt/me-tracker-data/.env`.
- `tailscale-oauth.env.example` → `secrets/tailscale-oauth.env` — the Tailscale
  OAuth client secret, one line:
  ```sh
  TS_OAUTH_SECRET=tskey-client-xxxxxxxx
  ```
- `secrets/deploy_key` (+ committed `secrets/deploy_key.pub`) — the SSH deploy
  key. Terraform injects the public key into the container; Ansible connects with
  the private key. Restore the private key from your password manager and
  `chmod 600` it.

The `.env`/OAuth source paths are `metracker_env_src` / `metracker_ts_oauth_src`
in `defaults/main.yml`; the key is referenced by `inventory.ini` (private) and
`infra/*-terraform.tfvars` (public). The playbook checks `.env` and the OAuth
secret exist up front and fails with a clear message if one is missing.

### Notes

- Re-runs are idempotent: `tailscale up` is skipped once the node is `Running`,
  and Funnel is only (re)set if port 5200 isn't already published.
- Unlike the app code, Tailscale config takes effect **without** pushing to
  `origin/master` — it runs from this control-machine checkout, not the box's
  clone. (The push rule only applies to app code that Ansible builds on the box.)
- `terraform destroy`/recreate leaves a stale node in the tailnet; delete it in
  the console or re-run the playbook to re-adopt the hostname.
