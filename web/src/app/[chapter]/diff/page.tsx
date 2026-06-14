import Link from "next/link";
import { CHAPTER_ORDER, CHAPTER_META } from "@/lib/constants";
import { LayerBadge } from "@/components/ui/badge";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { CodeDiff } from "@/components/diff/code-diff";
import { ArrowLeft, FileCode, Wrench, Box, FunctionSquare } from "lucide-react";
import chapterData from "@/data/generated/chapters.json";

export function generateStaticParams() {
  return CHAPTER_ORDER.map((chapter) => ({ chapter }));
}

export default async function DiffPage({
  params,
}: {
  params: Promise<{ chapter: string }>;
}) {
  const { chapter } = await params;
  const meta = CHAPTER_META[chapter];
  const current = chapterData.chapters.find((c) => c.id === chapter);
  const diff = chapterData.diffs.find((d) => d.to === chapter);
  const prev = diff?.from
    ? chapterData.chapters.find((c) => c.id === diff.from)
    : null;
  const prevMeta = diff?.from ? CHAPTER_META[diff.from] : null;

  if (!meta || !current) {
    return (
      <div className="py-12 text-center">
        <p className="text-[var(--color-text-secondary)]">Chapter not found.</p>
        <Link
          href="/timeline"
          className="mt-4 inline-block text-sm text-blue-600 hover:underline"
        >
          Back to timeline
        </Link>
      </div>
    );
  }

  if (!diff || !prev) {
    return (
      <div className="py-12">
        <Link
          href={`/${chapter}`}
          className="mb-6 inline-flex items-center gap-1 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text)]"
        >
          <ArrowLeft size={14} />
          Back to {meta.title}
        </Link>
        <h1 className="text-3xl font-bold">{meta.title}</h1>
        <p className="mt-4 text-[var(--color-text-secondary)]">
          This is the first chapter — there is no previous chapter to compare against.
        </p>
      </div>
    );
  }

  return (
    <div className="py-4">
      <Link
        href={`/${chapter}`}
        className="mb-6 inline-flex items-center gap-1 text-sm text-[var(--color-text-secondary)] hover:text-[var(--color-text)]"
      >
        <ArrowLeft size={14} />
        Back to {meta.title}
      </Link>

      <div className="mb-8">
        <h1 className="text-3xl font-bold">
          {prevMeta?.title} → {meta.title}
        </h1>
        <p className="mt-2 text-[var(--color-text-secondary)]">
          {prev.id} ({prev.loc} LOC) → {chapter} ({current.loc} LOC)
        </p>
      </div>

      <div className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
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
                diff.locDelta >= 0
                  ? "text-green-600 dark:text-green-400"
                  : "text-red-600 dark:text-red-400"
              }
            >
              {diff.locDelta >= 0 ? "+" : ""}
              {diff.locDelta}
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
              <span className="text-sm">New Tools</span>
            </div>
          </CardHeader>
          <CardTitle>
            <span className="text-blue-600 dark:text-blue-400">
              {diff.newTools.length}
            </span>
          </CardTitle>
          {diff.newTools.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-1">
              {diff.newTools.map((tool) => (
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
              <span className="text-sm">New Classes</span>
            </div>
          </CardHeader>
          <CardTitle>
            <span className="text-purple-600 dark:text-purple-400">
              {diff.newClasses.length}
            </span>
          </CardTitle>
          {diff.newClasses.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-1">
              {diff.newClasses.map((cls) => (
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
              <span className="text-sm">New Functions</span>
            </div>
          </CardHeader>
          <CardTitle>
            <span className="text-amber-600 dark:text-amber-400">
              {diff.newFunctions.length}
            </span>
          </CardTitle>
          {diff.newFunctions.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-1">
              {diff.newFunctions.map((fn) => (
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

      <div className="mb-8 grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Card className="border-l-4 border-l-red-300 dark:border-l-red-700">
          <CardHeader>
            <CardTitle>{prevMeta?.title}</CardTitle>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {prevMeta?.subtitle}
            </p>
          </CardHeader>
          <div className="space-y-1 text-sm text-[var(--color-text-secondary)]">
            <p>{prev.loc} LOC</p>
            <p>{prev.tools.length} tools: {prev.tools.join(", ")}</p>
            {prevMeta && (
              <LayerBadge layer={prevMeta.layer}>{prevMeta.layer}</LayerBadge>
            )}
          </div>
        </Card>
        <Card className="border-l-4 border-l-green-300 dark:border-l-green-700">
          <CardHeader>
            <CardTitle>{meta.title}</CardTitle>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {meta.subtitle}
            </p>
          </CardHeader>
          <div className="space-y-1 text-sm text-[var(--color-text-secondary)]">
            <p>{current.loc} LOC</p>
            <p>{current.tools.length} tools: {current.tools.join(", ")}</p>
            <LayerBadge layer={meta.layer}>{meta.layer}</LayerBadge>
          </div>
        </Card>
      </div>

      <div>
        <h2 className="mb-4 text-xl font-semibold">Source Code Diff</h2>
        <CodeDiff
          oldSource={prev.source}
          newSource={current.source}
          oldLabel={`${prev.id} (${prev.filename})`}
          newLabel={`${chapter} (${current.filename})`}
        />
      </div>
    </div>
  );
}
