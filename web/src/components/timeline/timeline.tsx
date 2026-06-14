"use client";

import Link from "next/link";
import { CHAPTER_ORDER, CHAPTER_META, LAYERS } from "@/lib/constants";
import { LayerBadge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { ChapterIndex } from "@/types/chapter-data";
import chapterDataRaw from "@/data/generated/chapters.json";

const chapterData = chapterDataRaw as ChapterIndex;

const LAYER_DOT_BG: Record<string, string> = {
  fundamentals: "bg-blue-500",
  "agent-core": "bg-emerald-500",
  memory: "bg-purple-500",
  reasoning: "bg-amber-500",
  advanced: "bg-red-500",
};

const LAYER_LINE_BG: Record<string, string> = {
  fundamentals: "bg-blue-500/30",
  "agent-core": "bg-emerald-500/30",
  memory: "bg-purple-500/30",
  reasoning: "bg-amber-500/30",
  advanced: "bg-red-500/30",
};

const LAYER_BAR_BG: Record<string, string> = {
  fundamentals: "bg-blue-500",
  "agent-core": "bg-emerald-500",
  memory: "bg-purple-500",
  reasoning: "bg-amber-500",
  advanced: "bg-red-500",
};

function getChapterData(id: string) {
  return chapterData.chapters.find((c) => c.id === id);
}

const MAX_LOC = Math.max(
  ...chapterData.chapters
    .filter((c) => CHAPTER_ORDER.includes(c.id as (typeof CHAPTER_ORDER)[number]))
    .map((c) => c.loc)
);

export function Timeline() {
  return (
    <div className="flex flex-col gap-12">
      {/* Layer Legend */}
      <div>
        <h3 className="mb-3 text-sm font-medium text-[var(--color-text-secondary)]">
          Layer Legend
        </h3>
        <div className="flex flex-wrap gap-3">
          {LAYERS.map((layer) => (
            <div key={layer.id} className="flex items-center gap-1.5">
              <span className={cn("h-3 w-3 rounded-full", LAYER_DOT_BG[layer.id])} />
              <span className="text-xs font-medium">{layer.label}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Vertical Timeline */}
      <div className="relative">
        {CHAPTER_ORDER.map((chapterId, index) => {
          const meta = CHAPTER_META[chapterId];
          const data = getChapterData(chapterId);
          if (!meta || !data) return null;

          const isLast = index === CHAPTER_ORDER.length - 1;
          const locPercent = Math.max(2, Math.round((data.loc / MAX_LOC) * 100));

          return (
            <div key={chapterId} className="relative flex gap-4 pb-8 sm:gap-6">
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    "z-10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full ring-4 ring-[var(--color-bg)] sm:h-10 sm:w-10",
                    LAYER_DOT_BG[meta.layer]
                  )}
                >
                  <span className="text-[10px] font-bold text-white sm:text-xs">
                    {chapterId}
                  </span>
                </div>
                {!isLast && (
                  <div
                    className={cn(
                      "w-0.5 flex-1",
                      LAYER_LINE_BG[
                        CHAPTER_META[CHAPTER_ORDER[index + 1]]?.layer || meta.layer
                      ]
                    )}
                  />
                )}
              </div>

              <div className="flex-1 pb-2">
                <Link href={`/${chapterId}`} className="group block">
                  <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-4 transition-colors hover:border-[var(--color-text-secondary)]/30 sm:p-5">
                    <div className="flex flex-wrap items-start gap-2">
                      <LayerBadge layer={meta.layer}>{chapterId}</LayerBadge>
                    </div>

                    <h3 className="mt-2 text-base font-semibold group-hover:underline sm:text-lg">
                      {meta.title}
                      <span className="ml-2 text-sm font-normal text-[var(--color-text-secondary)]">
                        {meta.subtitle}
                      </span>
                    </h3>

                    <div className="mt-3 flex flex-wrap items-center gap-4 text-xs text-[var(--color-text-secondary)]">
                      <span className="tabular-nums">{data.loc} LOC</span>
                      <span className="tabular-nums">{data.tools.length} tools</span>
                    </div>

                    <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-800">
                      <div
                        className={cn(
                          "h-full rounded-full transition-all",
                          LAYER_BAR_BG[meta.layer]
                        )}
                        style={{ width: `${locPercent}%` }}
                      />
                    </div>

                    <p className="mt-3 text-sm text-[var(--color-text-secondary)]">
                      {meta.description}
                    </p>
                  </div>
                </Link>
              </div>
            </div>
          );
        })}
      </div>

      {/* LOC Growth Chart */}
      <div>
        <h3 className="mb-4 text-lg font-semibold">LOC Growth</h3>
        <div className="flex flex-col gap-2">
          {CHAPTER_ORDER.map((chapterId) => {
            const meta = CHAPTER_META[chapterId];
            const data = getChapterData(chapterId);
            if (!meta || !data) return null;

            const widthPercent = Math.max(
              2,
              Math.round((data.loc / MAX_LOC) * 100)
            );

            return (
              <Link key={chapterId} href={`/${chapterId}`} className="group">
                <div className="flex items-center gap-3">
                  <span className="w-8 shrink-0 text-right text-xs font-medium tabular-nums text-[var(--color-text-secondary)]">
                    {chapterId}
                  </span>
                  <div className="flex-1">
                    <div className="h-5 w-full overflow-hidden rounded bg-zinc-100 dark:bg-zinc-800">
                      <div
                        className={cn(
                          "flex h-full items-center rounded px-2 transition-all",
                          LAYER_BAR_BG[meta.layer]
                        )}
                        style={{ width: `${widthPercent}%` }}
                      >
                        <span className="text-[10px] font-medium text-white">
                          {data.loc}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      </div>
    </div>
  );
}
