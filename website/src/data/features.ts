export type Feature = {
  title: string;
  description: string;
};

export const features: Feature[] = [
  {
    title: 'Centralized Collection',
    description:
      'Aggregate container, cloud, and on-prem application logs into a self-hosted pipeline with consistent enrichment and full control.',
  },
  {
    title: 'Fast Search',
    description:
      'Powerful Lucene queries over structured and unstructured data with saved views for incident response.',
  },
  {
    title: 'Real-Time Monitoring',
    description:
      'Live tail streaming and anomaly-triggered alerts keep teams ahead of production regressions.',
  },
  {
    title: 'Dashboards & Reports',
    description:
      'Purpose-built visualizations highlight trends, compliance evidence, and service health at a glance.',
  },
];

export const outcomes: Feature[] = [
  {
    title: 'Resolve issues faster',
    description:
      'Correlate logs with context so engineers spend less time spelunking and more time fixing root causes.',
  },
  {
    title: 'Stay compliant',
    description:
      'Immutable retention policies and automated redaction keep audit trails available without exposing secrets.',
  },
  {
    title: 'Scale with confidence',
    description:
      'Shard-aware ingestion and tiered storage grow with your traffic while keeping costs predictable.',
  },
];

export const integrations: string[] = [
  'OpenTelemetry Collector',
  'Fluent Bit & Fluentd',
  'Elastic Beats',
  'Kubernetes',
  'AWS, Azure, and GCP',
  'Syslog & Windows Event Forwarders',
];
