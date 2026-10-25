# M900 (ThinkCentre) environment — NON-SECRET config. Committed.
# Secrets (passwords) live in m900-secrets.tfvars (gitignored, in your password
# manager). Neither file is auto-loaded, so pass both explicitly — or just use
# the wrapper: ./apply-m900.sh  (which also selects the `m900` workspace).
#
#   terraform apply -var-file=m900-terraform.tfvars -var-file=m900-secrets.tfvars

# --- Proxmox connection ---
proxmox_endpoint = "https://192.168.1.69:8006/"
proxmox_node     = "pvem900"

# The Ubuntu template is downloaded by Terraform (see template_url /
# template_datastore_id defaults in variables.tf); override only if needed.

# --- Container placement / sizing ---
datastore_id         = "local-lvm"
storage_datastore_id = "local-lvm"
data_size_gb         = 1

# LXC OS hostname. Overridden to a box-specific name (default is "me-tracker")
# so it aligns with the Tailscale node name set in the Ansible inventory
# (metracker_ts_hostname=me-tracker-m900). The canonical "me-tracker" name is
# left unclaimed for now — to be assigned when the frontend cuts over from the
# T14 node. T14 runs as me-tracker-t14 and keeps working until decommissioned.
hostname = "me-tracker-m900"

# --- Networking (static so the Ansible inventory points at a known address) ---
# .221 is the M900 me-tracker LXC in the .220–.229 Proxmox/IaC block (see the
# ubiqiti-network repo's ip-allocation.md); .220 is the M900 Frigate LXC.
container_ip_cidr = "192.168.1.221/24"
container_gateway = "192.168.1.1"
dns_servers       = ["192.168.1.1"]

# --- Access ---
# SSH public key injected into the container's root account; Ansible connects
# with the matching private key. Lives in the repo (public key committed, private
# key gitignored). Path is relative to the infra/ working dir.
ssh_public_key_path = "../ansible/secrets/deploy_key.pub"
