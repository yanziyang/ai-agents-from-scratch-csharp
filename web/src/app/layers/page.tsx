"use client";

import Link from "next/link";
import { LAYERS, CHAPTER_META } from "@/lib/constants";
import { Card } from "@/components/ui/card";
import { LayerBadge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { ChevronRight } from "lucide-react";
import type { ChapterIndex } from "@/types/chapter-data";
import chapterData from "@/data/generated/chapters.json";

const data = chapterData as ChapterIndex;

const LAYER_BORDER_CLASSES: Record<string, string> = {
  fundamentals: "border-l-blue-500",
  "agent-core": "border-l-emerald-500",
  memory: "border-l-purple-500",
  reasoning: "border-l-amber-500",
  advanced: "border-l-red-500",
};

const LAYER_HEADER_BG: Record<string, string> = {
  fundamentals: "bg-blue-500",
  "agent-core": "bg-emerald-500",
  memory: "bg-purple-500",
  reasoning: "bg-amber-500",
  advanced: "bg-red-500",
};

export default function LayersPage() {
  return (
    <div className="py-4">
      <div className="mb-10">
        <h1 className="text-3xl font-bold">Architectural Layers</h1>
        <p className="mt-2 text-[var(--color-text-secondary)]">
          Five layers that build from a raw LLM call to advanced agent patterns
        </p>
      </div>

      <div className="space-y-6">
        {LAYERS.map((layer, index) => {
          const chapterInfos = layer.chapters.map((chapterId) => {
            const info = data.chapters.find((c) => c.id === chapterId);
            const meta = CHAPTER_META[chapterId];
            return { id: chapterId, info, meta };
          });

          return (
            <div
              key={layer.id}
              className={cn(
                "overflow-hidden rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)]",
                "border-l-4",
                LAYER_BORDER_CLASSES[layer.id]
              )}
            >
              <div className="flex items-center gap-3 px-6 py-4">
                <div
                  className={cn("h-3 w-3 rounded-full", LAYER_HEADER_BG[layer.id])}
                />
                <div>
                  <h2 className="text-xl font-bold">
                    <span className="text-[var(--color-text-secondary)]">
                      L{index + 1}
                    </span>{" "}
                    {layer.label}
                  </h2>
                </div>
              </div>

              <div className="border-t border-[var(--color-border)] bg-[var(--color-bg-secondary)]/30 px-6 py-4 dark:bg-zinc-900/30">
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {chapterInfos.map(({ id, info, meta }) => (
                    <Link key={id} href={`/${id}`} className="group">
                      <Card className="transition-shadow hover:shadow-md">
                        <div className="flex items-start justify-between">
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-2">
                              <span className="font-mono text-xs text-[var(--color-text-secondary)]">
                                {id}
                              </span>
                              <LayerBadge layer={layer.id}>{layer.id}</LayerBadge>
                            </div>
                            <h3 className="mt-1 font-semibold text-[var(--color-text)]">
                              {meta?.title}
                            </h3>
                            {meta?.subtitle && (
                              <p className="mt-0.5 text-xs text-[var(--color-text-secondary)]">
                                {meta.subtitle}
                              </p>
                            )}
                          </div>
                          <ChevronRight
                            size={16}
                            className="mt-1 shrink-0 text-zinc-300 transition-colors group-hover:text-zinc-600 dark:text-zinc-600 dark:group-hover:text-zinc-300"
                          />
                        </div>
                        <div className="mt-3 flex items-center gap-4 text-xs text-[var(--color-text-secondary)]">
                          <span>{info?.loc ?? "?"} LOC</span>
                          <span>{info?.tools.length ?? "?"} tools</span>
                        </div>
                        {meta?.description && (
                          <p className="mt-2 line-clamp-2 text-xs leading-relaxed text-[var(--color-text-secondary)]">
                            {meta.description}
                          </p>
                        )}
                      </Card>
                    </Link>
                  ))}
                </div>
              </div>

              {index < LAYERS.length - 1 && (
                <div className="flex items-center justify-center py-1 text-[var(--color-border)]">
                  <svg
                    width="20"
                    height="12"
                    viewBox="0 0 20 12"
                    fill="none"
                    className="text-current"
                  >
                    <path
                      d="M10 0 L10 12 M5 7 L10 12 L15 7"
                      stroke="currentColor"
                      strokeWidth="1.5"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}
