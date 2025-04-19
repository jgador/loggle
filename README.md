# Loggle

Loggle is a self-hosted log monitoring solution that stitches together the best available tools for log management. If you're looking to take control of your logs without relying on third-party services, Loggle is for you. This is a fun project intended for experimentation and learning, and it is not recommended for production use.

## Quick Start - Local Development

Before diving into cloud deployment, try Loggle locally:

1. **Prerequisites:**
   - Docker Desktop installed and running
   - Visual Studio or VS Code with .NET SDK

2. **Run with Docker:**
   ```powershell
   cd examples\Examples.Loggle.Console
   .\dc.ps1 start   # Starts all required containers
   ```
   This will provision:
   - Elasticsearch
   - Kibana
   - OpenTelemetry Collector

3. **Run the Example App:**
   - Open `Loggle.sln` in Visual Studio
   - Set `Examples.Loggle.Console` as startup project
   - Run the application (F5)

4. **View Your Logs:**
   - Open [Kibana Log Explorer](http://localhost:5601/app/observability-logs-explorer/)
   - Watch your logs flow in real-time

5. **Cleanup:**
   ```powershell
   .\dc.ps1 stop    # Stops and removes all containers
   ```

## Video Tutorial

Watch this short video on Google Drive for a walkthrough of setting up and using Loggle:  
[![Loggle Setup Video](https://drive.google.com/thumbnail?sz=w720&id=1uOmeeH3Hq63jPdic1IZwZl8jC4rPobLj)](https://drive.google.com/file/d/1uOmeeH3Hq63jPdic1IZwZl8jC4rPobLj/view?usp=drive_link)

This video provides a concise overview of deploying Loggle, configuring log forwarding, and accessing Kibana for log visualization.

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

## Data Flow

Your applications forward their logs to the OpenTelemetry Collector, which exports them to the Log Ingestion API. The Log Ingestion API processes the data and stores it in Elasticsearch, from where Kibana pulls the data for visualization.

```plaintext
+------------------+      +-------------------------+      +-------------------+      +---------------+      +--------+
| Application Logs | ---> | OpenTelemetry Collector | ---> | Log Ingestion API | ---> | Elasticsearch | ---> | Kibana |
+------------------+      +-------------------------+      +-------------------+      +---------------+      +--------+
```

## Cloud Deployment Guide
> **Prerequisite:**  
> Ensure you have Terraform with Azure CLI working. For more information, refer to [this guide](https://learn.microsoft.com/en-us/azure/developer/terraform/get-started-windows-bash).

> **Important Note:** The SSL certificate generation is currently hardcoded to use "kibana.loggle.co". You'll need to manually modify this in the deployment scripts if you're using a different domain. This will be made configurable in future updates.

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