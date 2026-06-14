"use client";

export function MessageFlow() {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-6">
      <div className="flex flex-col items-center gap-4">
        <div className="rounded-lg bg-blue-100 px-4 py-2 text-sm font-medium text-blue-800 dark:bg-blue-900/30 dark:text-blue-300">
          User
        </div>
        <div className="h-6 w-0.5 bg-zinc-300 dark:bg-zinc-700" />
        <div className="rounded-lg bg-emerald-100 px-4 py-2 text-sm font-medium text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300">
          LLM
        </div>
        <div className="h-6 w-0.5 bg-zinc-300 dark:bg-zinc-700" />
        <div className="rounded-lg bg-amber-100 px-4 py-2 text-sm font-medium text-amber-800 dark:bg-amber-900/30 dark:text-amber-300">
          Tools
        </div>
        <div className="h-6 w-0.5 bg-zinc-300 dark:bg-zinc-700" />
        <div className="rounded-lg bg-zinc-100 px-4 py-2 text-sm font-medium text-zinc-800 dark:bg-zinc-800 dark:text-zinc-300">
          Response
        </div>
      </div>
    </div>
  );
}
