variable "location" {
  default = "UK South"
}

variable service_name {
  default = "me-tracker"
}

variable "api_keys" {
    description = "API keys for the web application"
    sensitive   = true
}