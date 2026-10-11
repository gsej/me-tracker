# me-tracker on Proxmox (Terraform)

Builds an Ubuntu 24.04 LXC on Proxmox for the me-tracker API. Terraform only
**creates the container**; installing Docker and deploying the API is done by
Ansible in `../ansible`. State is stored locally for now.

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
2. The Proxmox `root@pam` password (in tfvars or `TF_VAR_proxmox_password`).
3. An SSH key present locally (defaults assume `~/.ssh/deploy/me-tracker`); the public
   key is injected into the container so Ansible can connect. Generate one with:

   ```sh
   mkdir -p ~/.ssh/deploy
   ssh-keygen -t ed25519 -f ~/.ssh/deploy/me-tracker -C "me-tracker-lxc" -N ""
   ```

## Usage

```sh
cd infra
cp terraform.tfvars.example terraform.tfvars   # then edit
terraform init
terraform plan
terraform apply
terraform output                               # note the container IP
```

Then deploy the API: see `../ansible/README.md`.
