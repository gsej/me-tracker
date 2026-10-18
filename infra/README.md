# me-tracker on Proxmox (Terraform)

Builds an Ubuntu 24.04 LXC on Proxmox for the me-tracker API. Terraform only
**creates the container**; installing Docker and deploying the API is done by
Ansible in `../ansible`. State is local, kept separate per environment via
Terraform workspaces (see below).

## Design

- **Unprivileged LXC** — the API is internet-facing (Tailscale Funnel, phase 2)
  and needs no device passthrough, so unprivileged gives stronger isolation.
  `nesting`+`keyctl` are still needed for Docker-in-LXC.
- **root@pam auth** — the provider authenticates with username/password, not an
  API token: tokens can't set feature flags like `keyctl`.
- **Self-contained template** — downloads its own Ubuntu template
  (`overwrite_unmanaged` adopts a copy another repo already fetched), so this
  repo doesn't depend on another one having run first.
- **Dedicated data mount** — the SQLite DB lives on its own volume at
  `/opt/me-tracker-data/database` so a container rebuild never destroys it.

## Prerequisites

1. A Proxmox node reachable over HTTPS. The Ubuntu template is downloaded by
   Terraform (adjust `template_url` if the pinned version 404s).
2. The Proxmox `root@pam` password (in `<env>-secrets.tfvars` or `TF_VAR_proxmox_password`).
3. A deploy SSH keypair at `../ansible/secrets/deploy_key{,.pub}`. The public key
   is committed and injected into the container; the private key is gitignored —
   restore it from your password manager (`chmod 600`), or generate a fresh pair:

   ```sh
   ssh-keygen -t ed25519 -f ../ansible/secrets/deploy_key -C "me-tracker-lxc" -N ""
   ```
   (If you regenerate, commit the new `deploy_key.pub`.)

## Environments, variables & state

Config is split per environment so secrets never get committed:

- `<env>-terraform.tfvars` — **committed**, non-secret config (endpoint, node,
  networking, hostname, …). `t14-terraform.tfvars` is the live T14 laptop.
- `<env>-secrets.tfvars` — **gitignored** (`infra/*-secrets.tfvars`): just the
  passwords; keep them in your password manager. Copy `secrets.tfvars.example`.
- Each environment uses its own **Terraform workspace**, so state is isolated
  under `terraform.tfstate.d/<env>/`.

Neither var file is auto-loaded (they aren't named `terraform.tfvars`), so they're
passed explicitly. The wrapper scripts do that *and* select the workspace:

```sh
cd infra
cp secrets.tfvars.example t14-secrets.tfvars   # then fill in from your password manager
terraform init
./plan-t14.sh
./apply-t14.sh
terraform output                               # note the container IP
# tear down:  ./apply-t14.sh -destroy
```

Then deploy the API: see `../ansible/README.md`.

### One-time: move the existing T14 state into the `t14` workspace

The current deployment's state lives in the `default` workspace. Migrate it once
so `./apply-t14.sh` manages it (otherwise the `t14` workspace starts empty and an
apply would try to recreate the container):

```sh
cd infra
terraform workspace new t14
cp terraform.tfstate terraform.tfstate.d/t14/terraform.tfstate
./plan-t14.sh          # should report: No changes
```

(The stale `default` state left at `infra/terraform.tfstate` is harmless.)

### Adding the M900 (later)

Copy `t14-terraform.tfvars` → `m900-terraform.tfvars` and edit it (new endpoint
and IP; **drop the `hostname` override** so it defaults to the canonical
`me-tracker`). Copy `secrets.tfvars.example` → `m900-secrets.tfvars`. Add
`plan-m900.sh` / `apply-m900.sh` (the `t14` scripts with `m900` swapped in) — they
run in the `m900` workspace, so its state is separate from T14's.
