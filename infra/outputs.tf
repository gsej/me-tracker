output "container_id" {
  description = "VMID of the me-tracker container"
  value       = proxmox_virtual_environment_container.metracker.vm_id
}

output "container_ip" {
  description = "IPv4 address of the container"
  value       = local.container_ip
}

output "api_url" {
  description = "me-tracker API on the LAN (phase 1)"
  value       = "http://${local.container_ip}:5200"
}
