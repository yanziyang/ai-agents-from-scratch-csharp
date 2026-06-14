"use client";

import { CHAPTER_ORDER, CHAPTER_META, LAYERS } from "@/lib/constants";
import { LayerBadge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const LAYER_BG: Record<string, string> = {
  fundamentals: "bg-blue-500/10 border-blue-500/30",
  "agent-core": "bg-emerald-500/10 border-emerald-500/30",
  memory: "bg-purple-500/10 border-purple-500/30",
  reasoning: "bg-amber-500/10 border-amber-500/30",
  advanced: "bg-red-500/10 border-red-500/30",
};

interface ChapterDiagramProps {
  chapterId: string;
}

export function ChapterDiagram({ chapterId }: ChapterDiagramProps) {
  const meta = CHAPTER_META[chapterId];
  if (!meta) return null;

  return (
    <div
      className={cn(
        "rounded-xl border p-6 text-center",
        LAYER_BG[meta.layer]
      )}
    >
      <LayerBadge layer={meta.layer}>{meta.layer}</LayerBadge>
      <h3 className="mt-3 text-lg font-semibold">{meta.title}</h3>
      <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
        {meta.subtitle}
      </p>
      <p className="mt-3 text-sm">{meta.description}</p>
      <div className="mt-4 flex items-center justify-center gap-2 text-xs text-[var(--color-text-secondary)]">
        <span>
          Chapter {parseInt(chapterId, 10)} of {CHAPTER_ORDER.length}
        </span>
      </div>
    </div>
  );
}

interface LearningPathDiagramProps {
  activeChapterId?: string;
}

export function LearningPathDiagram({ activeChapterId }: LearningPathDiagramProps) {
  return (
    <div className="flex flex-col gap-2">
      {LAYERS.map((layer) => (
        <div
          key={layer.id}
          className={cn(
            "rounded-lg border px-4 py-3",
            LAYER_BG[layer.id]
          )}
        >
          <div className="flex items-center gap-2">
            <div
              className="h-2.5 w-2.5 rounded-full"
              style={{ backgroundColor: layer.color }}
            />
            <span className="text-sm font-semibold">{layer.label}</span>
          </div>
          <div className="mt-2 flex flex-wrap gap-1.5">
            {layer.chapters.map((chapterId) => {
              const meta = CHAPTER_META[chapterId];
              const isActive = chapterId === activeChapterId;
              return (
                <span
                  key={chapterId}
                  className={cn(
                    "rounded px-2 py-0.5 text-xs",
                    isActive
                      ? "bg-zinc-900 text-white dark:bg-white dark:text-zinc-900"
                      : "bg-[var(--color-bg)] text-[var(--color-text-secondary)]"
                  )}
                >
                  {chapterId}: {meta?.title}
                </span>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
