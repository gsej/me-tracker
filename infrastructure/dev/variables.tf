variable "location" {
  default = "UK South"
}

variable service_name {
  default = "me-tracker"
}

variable "api_key" {
    description = "API key for the web application"
    sensitive   = true
}