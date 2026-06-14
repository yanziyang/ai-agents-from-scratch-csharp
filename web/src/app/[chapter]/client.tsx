"use client";

import { DocRenderer } from "@/components/docs/doc-renderer";
import { SourceViewer } from "@/components/code/source-viewer";
import { CodeDiff } from "@/components/diff/code-diff";
import { ChapterDiagram } from "@/components/architecture/chapter-diagram";
import { MessageFlow } from "@/components/architecture/message-flow";
import { Tabs } from "@/components/ui/tabs";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { FileCode, Wrench, Box, FunctionSquare } from "lucide-react";
import type { ChapterDiff } from "@/types/chapter-data";
import chapterData from "@/data/generated/chapters.json";

interface ChapterDetailClientProps {
  chapter: string;
  diff: ChapterDiff | null;
  source: string;
  filename: string;
  codeDoc: string;
  conceptDoc: string;
}

export function ChapterDetailClient({
  chapter,
  diff,
  source,
  filename,
  codeDoc,
  conceptDoc,
}: ChapterDetailClientProps) {
  const tabs = [
    { id: "learn", label: "Learn" },
    { id: "concepts", label: "Concepts" },
    { id: "code", label: "Code" },
    { id: "diff", label: "Diff" },
  ];

  const prevChapter = diff?.from;
  const prevSource = prevChapter
    ? chapterData.chapters.find((c) => c.id === prevChapter)?.source ?? ""
    : "";
  const prevFilename = prevChapter
    ? chapterData.chapters.find((c) => c.id === prevChapter)?.filename ?? ""
    : "";

  return (
    <div className="space-y-6">
      <ChapterDiagram chapterId={chapter} />

      <Tabs tabs={tabs} defaultTab="learn">
        {(activeTab) => (
          <>
            {activeTab === "learn" && (
              <>
                {codeDoc ? (
                  <DocRenderer content={codeDoc} />
                ) : (
                  <p className="text-[var(--color-text-secondary)]">
                    No code explanation available.
                  </p>
                )}
              </>
            )}
            {activeTab === "concepts" && (
              <>
                {conceptDoc ? (
                  <DocRenderer content={conceptDoc} />
                ) : (
                  <div className="space-y-6">
                    <MessageFlow />
                    <p className="text-[var(--color-text-secondary)]">
                      No concept document available yet.
                    </p>
                  </div>
                )}
              </>
            )}
            {activeTab === "code" && (
              <SourceViewer source={source} filename={filename} />
            )}
            {activeTab === "diff" && (
              <>
                {diff && prevChapter ? (
                  <div className="space-y-6">
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

                    <CodeDiff
                      oldSource={prevSource}
                      newSource={source}
                      oldLabel={`${prevChapter} (${prevFilename})`}
                      newLabel={`${chapter} (${filename})`}
                    />
                  </div>
                ) : (
                  <p className="text-[var(--color-text-secondary)]">
                    This is the first chapter — there is no previous chapter to compare against.
                  </p>
                )}
              </>
            )}
          </>
        )}
      </Tabs>
    </div>
  );
}
