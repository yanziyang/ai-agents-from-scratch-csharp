"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LAYERS, CHAPTER_META } from "@/lib/constants";
import { LayerBadge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const LAYER_DOT_BG: Record<string, string> = {
  fundamentals: "bg-blue-500",
  "agent-core": "bg-emerald-500",
  memory: "bg-purple-500",
  reasoning: "bg-amber-500",
  advanced: "bg-red-500",
};

export function Sidebar() {
  const pathname = usePathname();

  return (
    <nav className="hidden w-56 shrink-0 md:block">
      <div className="sticky top-[calc(3.5rem+2rem)] max-h-[calc(100vh-6rem)] space-y-5 overflow-y-auto pr-2">
        {LAYERS.map((layer) => (
          <div key={layer.id}>
            <div className="flex items-center gap-1.5 pb-1.5">
              <span
                className={cn("h-2 w-2 rounded-full", LAYER_DOT_BG[layer.id])}
              />
              <span className="text-[11px] font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500">
                {layer.label}
              </span>
            </div>
            <ul className="space-y-0.5">
              {layer.chapters.map((chapterId) => {
                const meta = CHAPTER_META[chapterId];
                const href = `/${chapterId}`;
                const isActive =
                  pathname === href ||
                  pathname === `${href}/` ||
                  pathname.startsWith(`${href}/`);

                return (
                  <li key={chapterId}>
                    <Link
                      href={href}
                      className={cn(
                        "block rounded-md px-2.5 py-1.5 text-sm transition-colors",
                        isActive
                          ? "bg-zinc-100 font-medium text-zinc-900 dark:bg-zinc-800 dark:text-white"
                          : "text-zinc-500 hover:bg-zinc-50 hover:text-zinc-700 dark:text-zinc-400 dark:hover:bg-zinc-800/50 dark:hover:text-zinc-300"
                      )}
                    >
                      <span className="font-mono text-xs">{chapterId}</span>
                      <span className="ml-1.5">{meta?.title}</span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </div>
    </nav>
  );
}
