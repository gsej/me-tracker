#!/usr/bin/env bash
# Terraform apply for the T14 environment: selects the `t14` workspace (separate
# state under terraform.tfstate.d/t14/) and loads the T14 var files.
# Tear down with: ./apply-t14.sh -destroy
set -euo pipefail
cd "$(dirname "$0")"
terraform workspace select t14 >/dev/null 2>&1 || terraform workspace new t14
terraform apply \
  -var-file=t14-terraform.tfvars \
  -var-file=t14-secrets.tfvars \
  "$@"
