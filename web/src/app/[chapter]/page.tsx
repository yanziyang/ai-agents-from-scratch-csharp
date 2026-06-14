import Link from "next/link";
import { CHAPTER_ORDER, CHAPTER_META, LAYERS } from "@/lib/constants";
import { LayerBadge } from "@/components/ui/badge";
import { ChapterDetailClient } from "./client";
import chapterData from "@/data/generated/chapters.json";
import docsData from "@/data/generated/docs.json";
import type { ChapterDoc } from "@/types/chapter-data";

export function generateStaticParams() {
  return CHAPTER_ORDER.map((chapter) => ({ chapter }));
}

export default async function ChapterPage({
  params,
}: {
  params: Promise<{ chapter: string }>;
}) {
  const { chapter } = await params;

  const chapterInfo = chapterData.chapters.find((c) => c.id === chapter);
  const meta = CHAPTER_META[chapter];
  const diff = chapterData.diffs.find((d) => d.to === chapter) ?? null;

  if (!chapterInfo || !meta) {
    return (
      <div className="py-20 text-center">
        <h1 className="text-2xl font-bold">Chapter not found</h1>
        <p className="mt-2 text-[var(--color-text-secondary)]">{chapter}</p>
      </div>
    );
  }

  const layer = LAYERS.find((l) => l.id === meta.layer);
  const pathIndex = CHAPTER_ORDER.indexOf(chapter as (typeof CHAPTER_ORDER)[number]);
  const prevChapter = pathIndex > 0 ? CHAPTER_ORDER[pathIndex - 1] : null;
  const nextChapter =
    pathIndex < CHAPTER_ORDER.length - 1 ? CHAPTER_ORDER[pathIndex + 1] : null;

  const codeDoc = (docsData as ChapterDoc[]).find(
    (d) => d.chapter === chapter && d.type === "code"
  );
  const conceptDoc = (docsData as ChapterDoc[]).find(
    (d) => d.chapter === chapter && d.type === "concept"
  );

  return (
    <div className="mx-auto max-w-3xl space-y-10 py-4">
      <header className="space-y-3">
        <div className="flex flex-wrap items-center gap-3">
          <span className="rounded-lg bg-zinc-100 px-3 py-1 font-mono text-lg font-bold dark:bg-zinc-800">
            {chapter}
          </span>
          <h1 className="text-2xl font-bold sm:text-3xl">{meta.title}</h1>
          {layer && <LayerBadge layer={meta.layer}>{layer.label}</LayerBadge>}
        </div>
        <p className="text-lg text-[var(--color-text-secondary)]">
          {meta.subtitle}
        </p>
        <div className="flex flex-wrap items-center gap-4 text-sm text-[var(--color-text-secondary)]">
          <span className="font-mono">{chapterInfo.loc} LOC</span>
          <span>{chapterInfo.tools.length} tools</span>
          {chapterInfo.classes.length > 0 && (
            <span>{chapterInfo.classes.length} classes</span>
          )}
          {chapterInfo.functions.length > 0 && (
            <span>{chapterInfo.functions.length} functions</span>
          )}
        </div>
        <blockquote className="border-l-4 border-[var(--color-border)] pl-4 text-sm italic text-[var(--color-text-secondary)]">
          {meta.description}
        </blockquote>
      </header>

      <ChapterDetailClient
        chapter={chapter}
        diff={diff}
        source={chapterInfo.source}
        filename={chapterInfo.filename}
        codeDoc={codeDoc?.content ?? ""}
        conceptDoc={conceptDoc?.content ?? ""}
      />

      <nav className="flex items-center justify-between border-t border-[var(--color-border)] pt-6">
        {prevChapter ? (
          <Link
            href={`/${prevChapter}`}
            className="group flex items-center gap-2 text-sm text-[var(--color-text-secondary)] transition-colors hover:text-[var(--color-text)]"
          >
            <span className="transition-transform group-hover:-translate-x-1">
              &larr;
            </span>
            <div>
              <div className="text-xs text-[var(--color-text-secondary)]">Previous</div>
              <div className="font-medium">
                {prevChapter} — {CHAPTER_META[prevChapter]?.title}
              </div>
            </div>
          </Link>
        ) : (
          <div />
        )}
        {nextChapter ? (
          <Link
            href={`/${nextChapter}`}
            className="group flex items-center gap-2 text-right text-sm text-[var(--color-text-secondary)] transition-colors hover:text-[var(--color-text)]"
          >
            <div>
              <div className="text-xs text-[var(--color-text-secondary)]">Next</div>
              <div className="font-medium">
                {CHAPTER_META[nextChapter]?.title} — {nextChapter}
              </div>
            </div>
            <span className="transition-transform group-hover:translate-x-1">
              &rarr;
            </span>
          </Link>
        ) : (
          <div />
        )}
      </nav>
    </div>
  );
}
