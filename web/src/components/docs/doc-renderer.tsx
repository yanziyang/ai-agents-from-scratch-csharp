"use client";

import { useEffect, useMemo, useRef } from "react";
import mermaid from "mermaid";
import { renderMarkdown, postProcessHtml } from "@/lib/markdown";

interface DocRendererProps {
  content: string;
}

export function DocRenderer({ content }: DocRendererProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  const html = useMemo(() => {
    const raw = renderMarkdown(content);
    return postProcessHtml(raw);
  }, [content]);

  useEffect(() => {
    mermaid.initialize({
      startOnLoad: false,
      theme: document.documentElement.classList.contains("dark") ? "dark" : "default",
      securityLevel: "loose",
    });

    if (containerRef.current) {
      const nodes = containerRef.current.querySelectorAll(".mermaid");
      if (nodes.length > 0) {
        mermaid.run({ nodes: Array.from(nodes) as HTMLElement[] }).catch(() => {
          // Mermaid syntax errors are rendered as text by the library.
        });
      }
    }
  }, [html]);

  return (
    <div className="py-4">
      <div
        ref={containerRef}
        className="prose-custom"
        dangerouslySetInnerHTML={{ __html: html }}
      />
    </div>
  );
}
