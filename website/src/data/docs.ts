export type DocHighlight = {
  title: string;
  summary: string;
  link: string;
};

const githubBase = 'https://github.com/jgador/loggle';

export const docHighlights: DocHighlight[] = [
  {
    title: 'Quick Start - Local Development',
    summary:
      'Spin up the full stack with Docker, explore the Aspire dashboard, and test multi-language log emitters.',
    link: `${githubBase}#quick-start---local-development`,
  },
  {
    title: 'Multilingual Logging Samples',
    summary:
      'Run ready-made PowerShell scripts to emit logs from .NET, Python, JavaScript, TypeScript, and Go against your collector.',
    link: `${githubBase}#multilingual-logging-samples`,
  },
  {
    title: '.NET Aspire Dashboard (Experimental)',
    summary:
      'Access the bundled Aspire UI, review exposed ports, and follow progress in the active loggle_aspire fork.',
    link: `${githubBase}#.net-aspire-dashboard`,
  },
  {
    title: 'What It Does',
    summary:
      'Review the core components — OpenTelemetry Collector, Elasticsearch, Kibana, and the supporting services that make up the Loggle stack.',
    link: `${githubBase}#what-it-does`,
  },
  {
    title: 'Data Flow',
    summary:
      'See how application logs move from language SDKs through the collector, ingestion API, and into Elasticsearch.',
    link: `${githubBase}#data-flow`,
  },
  {
    title: 'Cloud Deployment Guide',
    summary:
      'Follow the Terraform playbook for provisioning Azure infrastructure, managing DNS, rerunning setup scripts, and handling certificates.',
    link: `${githubBase}#cloud-deployment-guide`,
  },
  {
    title: 'Operational Tips',
    summary:
      'Re-run the provisioning script safely and manage SSL automation, firewall allow lists, and teardown helpers once Loggle is live.',
    link: `${githubBase}#re-run-the-provisioning-script-inside-the-vm`,
  },
];

export const repositoryUrl = githubBase;
