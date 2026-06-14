"use client";

import { useMemo } from "react";
import { createTwoFilesPatch } from "diff";

interface CodeDiffProps {
  oldSource: string;
  newSource: string;
  oldLabel: string;
  newLabel: string;
}

export function CodeDiff({ oldSource, newSource, oldLabel, newLabel }: CodeDiffProps) {
  const patch = useMemo(() => {
    return createTwoFilesPatch(
      oldLabel,
      newLabel,
      oldSource,
      newSource,
      undefined,
      undefined,
      { context: 3 }
    );
  }, [oldSource, newSource, oldLabel, newLabel]);

  const lines = useMemo(() => patch.split("\n").slice(1), [patch]);

  return (
    <div className="rounded-lg border border-zinc-200 dark:border-zinc-700">
      <div className="flex items-center gap-2 border-b border-zinc-200 bg-zinc-100 px-4 py-2 dark:border-zinc-700 dark:bg-zinc-800">
        <div className="flex gap-1.5">
          <span className="h-3 w-3 rounded-full bg-red-400" />
          <span className="h-3 w-3 rounded-full bg-yellow-400" />
          <span className="h-3 w-3 rounded-full bg-green-400" />
        </div>
        <span className="font-mono text-xs text-zinc-600 dark:text-zinc-400">
          {oldLabel} → {newLabel}
        </span>
      </div>
      <div className="overflow-x-auto bg-zinc-950">
        <pre className="p-2 text-[10px] leading-4 sm:p-4 sm:text-xs sm:leading-5">
          <code>
            {lines.map((line, i) => {
              let bgClass = "";
              if (line.startsWith("+")) bgClass = "bg-green-500/10";
              else if (line.startsWith("-")) bgClass = "bg-red-500/10";
              else if (line.startsWith("@@")) bgClass = "text-zinc-500";

              let textClass = "text-zinc-300";
              if (line.startsWith("+")) textClass = "text-green-400";
              else if (line.startsWith("-")) textClass = "text-red-400";
              else if (line.startsWith("@@")) textClass = "text-amber-400";

              return (
                <div key={i} className={`flex ${bgClass}`}>
                  <span className="text-zinc-600 select-none">{line}</span>
                </div>
              );
            })}
          </code>
        </pre>
      </div>
    </div>
  );
}
