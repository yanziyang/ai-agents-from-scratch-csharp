"use client";

import { useState, useMemo } from "react";
import { CHAPTER_ORDER, CHAPTER_META } from "@/lib/constants";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { LayerBadge } from "@/components/ui/badge";
import { CodeDiff } from "@/components/diff/code-diff";
import { ArrowRight, FileCode, Wrench, Box, FunctionSquare } from "lucide-react";
import type { ChapterIndex } from "@/types/chapter-data";
import chapterData from "@/data/generated/chapters.json";

const data = chapterData as ChapterIndex;

export default function ComparePage() {
  const [chapterA, setChapterA] = useState<string>("");
  const [chapterB, setChapterB] = useState<string>("");

  const infoA = useMemo(
    () => data.chapters.find((c) => c.id === chapterA),
    [chapterA]
  );
  const infoB = useMemo(
    () => data.chapters.find((c) => c.id === chapterB),
    [chapterB]
  );
  const metaA = chapterA ? CHAPTER_META[chapterA] : null;
  const metaB = chapterB ? CHAPTER_META[chapterB] : null;

  const comparison = useMemo(() => {
    if (!infoA || !infoB) return null;
    const toolsA = new Set(infoA.tools);
    const toolsB = new Set(infoB.tools);
    const onlyA = infoA.tools.filter((t) => !toolsB.has(t));
    const onlyB = infoB.tools.filter((t) => !toolsA.has(t));
    const shared = infoA.tools.filter((t) => toolsB.has(t));

    const classesA = new Set(infoA.classes.map((c) => c.name));
    const newClasses = infoB.classes
      .map((c) => c.name)
      .filter((c) => !classesA.has(c));

    const funcsA = new Set(infoA.functions.map((f) => f.name));
    const newFunctions = infoB.functions
      .map((f) => f.name)
      .filter((f) => !funcsA.has(f));

    return {
      locDelta: infoB.loc - infoA.loc,
      toolsOnlyA: onlyA,
      toolsOnlyB: onlyB,
      toolsShared: shared,
      newClasses,
      newFunctions,
    };
  }, [infoA, infoB]);

  return (
    <div className="py-4">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">Compare Chapters</h1>
        <p className="mt-2 text-[var(--color-text-secondary)]">
          See what changed between any two chapters
        </p>
      </div>

      <div className="mb-8 flex flex-col items-start gap-4 sm:flex-row sm:items-center">
        <div className="flex-1">
          <label className="mb-1 block text-sm font-medium text-[var(--color-text-secondary)]">
            Chapter A
          </label>
          <select
            value={chapterA}
            onChange={(e) => setChapterA(e.target.value)}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm text-[var(--color-text)]"
          >
            <option value="">-- select --</option>
            {CHAPTER_ORDER.map((c) => (
              <option key={c} value={c}>
                {c} — {CHAPTER_META[c]?.title}
              </option>
            ))}
          </select>
        </div>

        <ArrowRight
          size={20}
          className="mt-5 hidden text-[var(--color-text-secondary)] sm:block"
        />

        <div className="flex-1">
          <label className="mb-1 block text-sm font-medium text-[var(--color-text-secondary)]">
            Chapter B
          </label>
          <select
            value={chapterB}
            onChange={(e) => setChapterB(e.target.value)}
            className="w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-bg)] px-3 py-2 text-sm text-[var(--color-text)]"
          >
            <option value="">-- select --</option>
            {CHAPTER_ORDER.map((c) => (
              <option key={c} value={c}>
                {c} — {CHAPTER_META[c]?.title}
              </option>
            ))}
          </select>
        </div>
      </div>

      {infoA && infoB && comparison && (
        <div className="space-y-8">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>{metaA?.title}</CardTitle>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  {metaA?.subtitle}
                </p>
              </CardHeader>
              <div className="space-y-2 text-sm text-[var(--color-text-secondary)]">
                <p>{infoA.loc} LOC</p>
                <p>{infoA.tools.length} tools</p>
                {metaA && <LayerBadge layer={metaA.layer}>{metaA.layer}</LayerBadge>}
              </div>
            </Card>
            <Card>
              <CardHeader>
                <CardTitle>{metaB?.title}</CardTitle>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  {metaB?.subtitle}
                </p>
              </CardHeader>
              <div className="space-y-2 text-sm text-[var(--color-text-secondary)]">
                <p>{infoB.loc} LOC</p>
                <p>{infoB.tools.length} tools</p>
                {metaB && <LayerBadge layer={metaB.layer}>{metaB.layer}</LayerBadge>}
              </div>
            </Card>
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2 text-[var(--color-text-secondary)]">
                  <FileCode size={16} />
                  <span className="text-sm">LOC Delta</span>
                </div>
              </CardHeader>
              <CardTitle>
                <span
                  className={
                    comparison.locDelta >= 0
                      ? "text-green-600 dark:text-green-400"
                      : "text-red-600 dark:text-red-400"
                  }
                >
                  {comparison.locDelta >= 0 ? "+" : ""}
                  {comparison.locDelta}
                </span>
                <span className="ml-2 text-sm font-normal text-[var(--color-text-secondary)]">
                  lines
                </span>
              </CardTitle>
            </Card>

            <Card>
              <CardHeader>
                <div className="flex items-center gap-2 text-[var(--color-text-secondary)]">
                  <Wrench size={16} />
                  <span className="text-sm">New Tools in B</span>
                </div>
              </CardHeader>
              <CardTitle>
                <span className="text-blue-600 dark:text-blue-400">
                  {comparison.toolsOnlyB.length}
                </span>
              </CardTitle>
              {comparison.toolsOnlyB.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {comparison.toolsOnlyB.map((tool) => (
                    <span
                      key={tool}
                      className="rounded bg-blue-100 px-1.5 py-0.5 text-xs text-blue-700 dark:bg-blue-900/30 dark:text-blue-300"
                    >
                      {tool}
                    </span>
                  ))}
                </div>
              )}
            </Card>

            <Card>
              <CardHeader>
                <div className="flex items-center gap-2 text-[var(--color-text-secondary)]">
                  <Box size={16} />
                  <span className="text-sm">New Classes in B</span>
                </div>
              </CardHeader>
              <CardTitle>
                <span className="text-purple-600 dark:text-purple-400">
                  {comparison.newClasses.length}
                </span>
              </CardTitle>
              {comparison.newClasses.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {comparison.newClasses.map((cls) => (
                    <span
                      key={cls}
                      className="rounded bg-purple-100 px-1.5 py-0.5 text-xs text-purple-700 dark:bg-purple-900/30 dark:text-purple-300"
                    >
                      {cls}
                    </span>
                  ))}
                </div>
              )}
            </Card>

            <Card>
              <CardHeader>
                <div className="flex items-center gap-2 text-[var(--color-text-secondary)]">
                  <FunctionSquare size={16} />
                  <span className="text-sm">New Functions in B</span>
                </div>
              </CardHeader>
              <CardTitle>
                <span className="text-amber-600 dark:text-amber-400">
                  {comparison.newFunctions.length}
                </span>
              </CardTitle>
              {comparison.newFunctions.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {comparison.newFunctions.map((fn) => (
                    <span
                      key={fn}
                      className="rounded bg-amber-100 px-1.5 py-0.5 text-xs text-amber-700 dark:bg-amber-900/30 dark:text-amber-300"
                    >
                      {fn}
                    </span>
                  ))}
                </div>
              )}
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>Tool Comparison</CardTitle>
            </CardHeader>
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
              <div>
                <h4 className="mb-2 text-sm font-medium text-[var(--color-text-secondary)]">
                  Only in {metaA?.title}
                </h4>
                {comparison.toolsOnlyA.length === 0 ? (
                  <p className="text-xs text-[var(--color-text-secondary)]">None</p>
                ) : (
                  <div className="flex flex-wrap gap-1">
                    {comparison.toolsOnlyA.map((tool) => (
                      <span
                        key={tool}
                        className="rounded bg-red-100 px-1.5 py-0.5 text-xs text-red-700 dark:bg-red-900/30 dark:text-red-300"
                      >
                        {tool}
                      </span>
                    ))}
                  </div>
                )}
              </div>
              <div>
                <h4 className="mb-2 text-sm font-medium text-[var(--color-text-secondary)]">
                  Shared
                </h4>
                {comparison.toolsShared.length === 0 ? (
                  <p className="text-xs text-[var(--color-text-secondary)]">None</p>
                ) : (
                  <div className="flex flex-wrap gap-1">
                    {comparison.toolsShared.map((tool) => (
                      <span
                        key={tool}
                        className="rounded bg-zinc-100 px-1.5 py-0.5 text-xs text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300"
                      >
                        {tool}
                      </span>
                    ))}
                  </div>
                )}
              </div>
              <div>
                <h4 className="mb-2 text-sm font-medium text-[var(--color-text-secondary)]">
                  Only in {metaB?.title}
                </h4>
                {comparison.toolsOnlyB.length === 0 ? (
                  <p className="text-xs text-[var(--color-text-secondary)]">None</p>
                ) : (
                  <div className="flex flex-wrap gap-1">
                    {comparison.toolsOnlyB.map((tool) => (
                      <span
                        key={tool}
                        className="rounded bg-green-100 px-1.5 py-0.5 text-xs text-green-700 dark:bg-green-900/30 dark:text-green-300"
                      >
                        {tool}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </Card>

          <div>
            <h2 className="mb-4 text-xl font-semibold">Source Code Diff</h2>
            <CodeDiff
              oldSource={infoA.source}
              newSource={infoB.source}
              oldLabel={`${infoA.id} (${infoA.filename})`}
              newLabel={`${infoB.id} (${infoB.filename})`}
            />
          </div>
        </div>
      )}

      {(!chapterA || !chapterB) && (
        <div className="rounded-lg border border-dashed border-[var(--color-border)] p-12 text-center">
          <p className="text-[var(--color-text-secondary)]">
            Select two chapters above to compare them.
          </p>
        </div>
      )}
    </div>
  );
}
