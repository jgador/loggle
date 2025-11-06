variable "vm_size" {
  description = "The size of the virtual machine."
  type        = string
  default     = "Standard_D2s_v3"
}

variable "ssh_key_path" {
  description = "The file path on the VM where the SSH public key is stored"
  type        = string
  default     = "/home/loggle/.ssh/authorized_keys"
}

variable "ssh_key_data" {
  description = "The SSH public key data"
  type        = string
  default     = "~/.ssh/loggle.pub"
}

variable "kibana_allowed_ips" {
  description = "List of IPv4 addresses or CIDR blocks allowed to access Kibana and related HTTPS endpoints."
  type        = list(string)
  default     = ["34.126.86.243"]
}
