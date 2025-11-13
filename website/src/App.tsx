import './App.css';
import {
  features,
  integrations,
  outcomes,
  type Feature,
} from './data/features';
import {
  docHighlights,
  repositoryUrl,
  type DocHighlight,
} from './data/docs';
import MermaidDiagram from './components/MermaidDiagram';

const SectionIntro = ({
  eyebrow,
  title,
  description,
}: {
  eyebrow?: string;
  title: string;
  description: string;
}) => (
  <header className="section__intro">
    {eyebrow && <p className="section__eyebrow">{eyebrow}</p>}
    <h2 className="section__title">{title}</h2>
    <p className="section__description">{description}</p>
  </header>
);

const FeatureList = ({ items }: { items: Feature[] }) => (
  <div className="feature-grid">
    {items.map(({ title, description }) => (
      <article className="feature-card" key={title}>
        <h3>{title}</h3>
        <p>{description}</p>
      </article>
    ))}
  </div>
);

const DocsList = ({ items }: { items: DocHighlight[] }) => (
  <div className="docs-grid">
    {items.map(({ title, summary, link }) => (
      <article className="docs-card" key={title}>
        <h3>{title}</h3>
        <p>{summary}</p>
        <a
          className="docs-card__link"
          href={link}
          target="_blank"
          rel="noreferrer"
        >
          Read section
          <span aria-hidden="true">{'\u2192'}</span>
        </a>
      </article>
    ))}
  </div>
);

function App() {
  const nugetSnippet = `dotnet add package Loggle`;

  const appsettingsSnippet = `{
  "Logging": {
    "OpenTelemetry": {
      "IncludeFormattedMessage": true,
      "IncludeScopes": true,
      "ParseStateValues": true
    },
    "Loggle": {
      "ServiceName": "Examples.Loggle.Console",
      "ServiceVersion": "v0.99.5-rc.7",
      "OtelCollector": {
        "BearerToken": "REPLACE_WITH_YOUR_OWN_SECRET",
        "LogsReceiverEndpoint": "http://your-domain-or-ip:4318/v1/logs"
      }
    }
  }
}`;

  const programSnippet = `var builder = Host.CreateDefaultBuilder(args)
  .ConfigureServices((hostContext, services) =>
  {
    // Register the Loggle exporter
    services.AddLoggleExporter();
  });`;

  const dataFlowDiagram = String.raw`flowchart TB
    csharp["C#"]
    go["Go"]
    javascript["JavaScript"]
    python["Python"]
    typescript["TypeScript"]
    others["Other"]

    subgraph sources["Application Logs"]
        csharp --> apps
        go --> apps
        javascript --> apps
        python --> apps
        typescript --> apps
        others --> apps
    end

    apps --> collector["OpenTelemetry Collector"]
    collector --> ingestion["Log Ingestion API"]
    ingestion --> elastic["Elasticsearch"]
    elastic --> kibana["Kibana"]
    elastic --> aspire[".NET Aspire Dashboard"]
  `;

  const currentYear = new Date().getFullYear();

  return (
    <div className="page">
      <header className="hero" id="top">
        <nav className="hero__nav">
          <a className="hero__brand" href="#top" aria-label="Go to top of page">
            <img
              className="hero__brand-logo"
              src="/logo.png"
              alt="Loggle logo"
            />
            Loggle
          </a>
          <div className="hero__nav-links">
            <a href="#why">Platform</a>
            <a href="#data-flow">Architecture</a>
            <a href="#dotnet">.NET</a>
            <a href="#docs">Docs</a>
            <a href="#integrations">Integrations</a>
            <a href={repositoryUrl} target="_blank" rel="noreferrer">
              GitHub
            </a>
          </div>
        </nav>

        <div className="hero__content">
          <div className="hero__copy">
            <div className="hero__badge">In active development - not ready for production</div>
            <p className="hero__eyebrow">Self-hosted log intelligence for the OTel era</p>
            <h1>
              Make sense of every log line<span> - without the clutter</span>
            </h1>
            <p className="hero__summary">
              Loggle is a self-hosted log management stack built on open
              standards. Deploy it on your own infrastructure, cut through the
              noise, surface the signals that matter, and keep your incident
              response calm.
            </p>

            <div className="hero__actions">
              <a className="hero__cta" href="#why">
                Explore the platform
              </a>
              <a className="hero__link" href="#dotnet">
                See .NET setup
              </a>
              <a
                className="hero__link hero__link--ghost"
                href={repositoryUrl}
                target="_blank"
                rel="noreferrer"
              >
                View on GitHub
              </a>
            </div>
          </div>

          <div className="hero__panel" aria-hidden="true">
            <div className="hero__panel-card">
              <p className="hero__panel-caption">Live Search Highlight</p>
              <div className="hero__panel-body">
                <span className="hero__chip">service:api-gateway</span>
                <span className="hero__chip hero__chip--accent">
                  severity:error
                </span>
                <span className="hero__chip">latency&gt;500ms</span>
                <p>
                  Filter instantly, export the trace IDs, and notify on-call in
                  a single view.
                </p>
              </div>
            </div>
          </div>
        </div>
      </header>

      <main>
        <section className="section" id="why">
          <SectionIntro
            eyebrow="Aligned with modern teams"
            title="A focused platform for everyday logging routines"
            description="Loggle keeps the essentials front and centre: clean navigation, fast searching, and straightforward operations that help teams stay productive."
          />

          <FeatureList items={features} />

          <div className="section__divider" role="presentation" />

          <SectionIntro
            eyebrow="Value on day one"
            title="Outcomes your stakeholders will notice"
            description="Translate advanced log analytics into business results with predictable operations, less toil, and stronger compliance posture."
          />
          <FeatureList items={outcomes} />
        </section>

        <section className="section section--diagram" id="data-flow">
          <SectionIntro
            eyebrow="Source to visualization"
            title="How Loggle moves your data"
            description="Language SDKs and agents forward logs to the OpenTelemetry Collector, the Log Ingestion API enriches them, and Elasticsearch powers the dashboards your teams rely on."
          />
          <MermaidDiagram
            chart={dataFlowDiagram}
            ariaLabel="Flowchart showing how Loggle receives logs from various applications, processes them, and serves Kibana and the .NET Aspire dashboard."
          />
          <p className="diagram-note">
            Connect anything that speaks OTLP or standard forwarders. The same
            data stream fuels Kibana for analysts and the experimental .NET
            Aspire dashboard for developers.
          </p>
        </section>

        <section className="section section--dotnet" id="dotnet">
          <SectionIntro
            eyebrow=".NET in minutes"
            title="Add Loggle to your existing .NET logger"
            description="Drop two small snippets into your application and you’re streaming structured logs into Loggle without refactoring your stack."
          />

          <div className="dotnet-install">
            <h3>Install from NuGet</h3>
            <pre className="code-block">
              <code>{nugetSnippet}</code>
            </pre>
            <p>
              Package Manager Console: <code>Install-Package Loggle</code>
            </p>
          </div>

          <div className="dotnet-steps">
            <div className="dotnet-step">
              <h3>
                Configure <code>appsettings.json</code>
              </h3>
              <pre className="code-block">
                <code>{appsettingsSnippet}</code>
              </pre>
            </div>
            <div className="dotnet-step">
              <h3>
                Register the exporter in <code>Program.cs</code>
              </h3>
              <pre className="code-block">
                <code>{programSnippet}</code>
              </pre>
            </div>
          </div>

          <p className="dotnet-note">
            Works with the standard <code>Host.CreateDefaultBuilder</code> setup—no custom logging pipelines required.
          </p>
        </section>

        <section className="section" id="docs">
          <SectionIntro
            eyebrow="Docs & how-to guides"
            title="Source of truth lives in the Loggle README"
            description="Skim the highlights below, then follow the links directly to the repository README for the full walkthroughs."
          />
          <DocsList items={docHighlights} />
        </section>

        <section className="section" id="integrations">
          <SectionIntro
            eyebrow="Open ecosystem"
            title="Ready to plug into your existing pipelines"
            description="Bring logs from cloud platforms, legacy systems, and modern workloads without juggling custom agents or brittle exporters."
          />

          <ul className="integration-list">
            {integrations.map(item => (
              <li className="integration-chip" key={item}>
                {item}
              </li>
            ))}
          </ul>

          <div className="integration-note">
            Loggle embraces open-source tooling. Forward the same OTLP stream to
            Loggle and a secondary sink for redundancy, or mirror traffic for
            comparisons against any tool listed above.
          </div>
        </section>
      </main>

      <footer className="footer">
        <p>&copy; {currentYear} Loggle.</p>
        <a className="footer__cta" href="#top">
          Back to top
        </a>
      </footer>
    </div>
  );
}

export default App;


