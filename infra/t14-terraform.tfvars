# T14 (temporary laptop) environment — NON-SECRET config. Committed.
# Secrets (passwords) live in t14-secrets.tfvars (gitignored, in your password
# manager). Neither file is auto-loaded, so pass both explicitly — or just use
# the wrapper: ./apply-t14.sh  (which also selects the `t14` workspace).
#
#   terraform apply -var-file=t14-terraform.tfvars -var-file=t14-secrets.tfvars

# --- Proxmox connection ---
proxmox_endpoint = "https://192.168.1.198:8006/"
proxmox_node     = "pve"

# The Ubuntu template is downloaded by Terraform (see template_url /
# template_datastore_id defaults in variables.tf); override only if needed.

# --- Container placement / sizing ---
datastore_id         = "local-lvm"
storage_datastore_id = "local-lvm"
data_size_gb         = 1

# LXC OS hostname. Default (variables.tf) is "me-tracker" — the canonical name
# reserved for the permanent M900 box. This laptop (T14) is a temporary stand-in,
# so override it here to keep the OS hostname aligned with the Tailscale node
# name (set in the Ansible inventory as metracker_ts_hostname=me-tracker-t14).
hostname = "me-tracker-t14"

# --- Networking (static so the Ansible inventory points at a known address) ---
container_ip_cidr = "192.168.1.132/24"
container_gateway = "192.168.1.1"
dns_servers       = ["192.168.1.1"]

# --- Access ---
# SSH public key injected into the container's root account; Ansible connects
# with the matching private key. Lives in the repo (public key committed, private
# key gitignored). Path is relative to the infra/ working dir.
ssh_public_key_path = "../ansible/secrets/deploy_key.pub"
