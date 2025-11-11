import { useEffect, useMemo, useRef } from 'react';
import mermaid from 'mermaid';

type MermaidDiagramProps = {
  chart: string;
  ariaLabel?: string;
};

let mermaidInitialized = false;

const MermaidDiagram = ({ chart, ariaLabel }: MermaidDiagramProps) => {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const renderId = useMemo(
    () => `mermaid-${Math.random().toString(36).slice(2, 9)}`,
    []
  );

  useEffect(() => {
    if (typeof window === 'undefined') {
      return;
    }

    if (!mermaidInitialized) {
      mermaid.initialize({ startOnLoad: false, theme: 'neutral' });
      mermaidInitialized = true;
    }

    let isActive = true;

    const renderDiagram = async () => {
      try {
        const { svg } = await mermaid.render(renderId, chart);

        if (isActive && containerRef.current) {
          containerRef.current.innerHTML = svg;
        }
      } catch (error) {
        console.error('Failed to render mermaid diagram', error);
      }
    };

    renderDiagram();

    return () => {
      isActive = false;
    };
  }, [chart, renderId]);

  return (
    <div
      ref={containerRef}
      className="mermaid-diagram"
      role="img"
      aria-label={ariaLabel}
    />
  );
};

export default MermaidDiagram;
