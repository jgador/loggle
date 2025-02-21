# Loggle

Loggle is a self-hosted log monitoring solution that stitches together the best available tools for log management. If you're looking to take control of your logs without relying on third-party services, Loggle is for you. This is a fun project intended for experimentation and learning, and it is not recommended for production use.

## What It Does

- **Self-Hosted Monitoring:** Manage your logs on your own server.
- **Complete Toolset:**  
  - **OpenTelemetry Collector:** Collects your logs.  
  - **Elasticsearch:** Stores your logs.  
  - **Kibana:** Visualizes your logs.
- **Easy Deployment:**  
  - Provision a virtual machine with Terraform on Azure (support for AWS and GCP coming soon).  
  - Automatically obtain and renew SSL/TLS certificates using Certbot with Let's Encrypt.
- **Simple Setup:** Provision your VM, send your logs, and access them in Kibana.

## Quick Start
> **Prerequisite:**  
> Ensure you have Terraform with Azure CLI working. For more information, refer to [this guide](https://learn.microsoft.com/en-us/azure/developer/terraform/get-started-windows-bash).


1. **Generate an SSH Key:**  
   The SSH key will be used to authenticate your virtual machine.  
   If you're using PowerShell, run:
    ```powershell
    ssh-keygen -t rsa -b 4096 -C "loggle" -f "$env:USERPROFILE\.ssh\loggle" -N ""
    ```

2. **Clone the Repository:**  
    ```bash
    git clone https://github.com/jgador/loggle
    cd terraform\azure
    ```

3. **Provision the Public IP:**  
    This will allocate a public IP for your VM.
    ```bash
    terraform apply -target="azurerm_public_ip.public_ip" -auto-approve
    ```

4. **Update Your Domain Registrar:**  
    Configure your domain's DNS settings by adding an **A record** that points to your public IP address with a TTL of 600 seconds. For example, in GoDaddy, go to your domain's DNS management panel, create a new **A** record with the host set to "@" (or your preferred subdomain), enter your public IP address, and set the TTL to 600.

5. **Deploy with Terraform:**  
    This step deploys all the necessary resources including the resource group, virtual network, subnet, public IP, network security group, network interface, and the virtual machine.
    ```bash
    terraform apply -auto-approve
    ```

6. **Send Your Logs:**  
    Set up your applications to forward logs to your public IP address on port **4318**. For example, in a .NET application, you can add the following code to send logs using the OpenTelemetry Collector:  
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder
        .Logging
        .AddOpenTelemetry(opt =>
        {
            opt.IncludeFormattedMessage = true;
            opt.ParseStateValues = true;
            opt.AddOtlpExporter(exporterOptions =>
            {
                exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                exporterOptions.Endpoint = new Uri("http://52.230.2.122:4318/v1/logs");
            });
        });
    ```

7. **Access Kibana:**  
    Kibana is automatically set up as part of the deployment and listens on port **5601**. Open your browser and navigate to your DNS name (for example, `kibana.loggle.co:5601`) to view your logs. Remember: the OpenTelemetry Collector listens on port **4318** and Kibana on port **5601**.