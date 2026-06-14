"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Github, Menu, X, Sun, Moon } from "lucide-react";
import { useState, useEffect } from "react";
import { cn } from "@/lib/utils";

const NAV_ITEMS = [
  { label: "Timeline", href: "/timeline" },
  { label: "Layers", href: "/layers" },
  { label: "Compare", href: "/compare" },
] as const;

export function Header() {
  const pathname = usePathname();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [dark, setDark] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    setDark(document.documentElement.classList.contains("dark"));
  }, []);

  function toggleDark() {
    const next = !dark;
    setDark(next);
    document.documentElement.classList.toggle("dark", next);
    localStorage.setItem("theme", next ? "dark" : "light");
  }

  return (
    <header className="sticky top-0 z-50 border-b border-[var(--color-border)] bg-[var(--color-bg)]/80 backdrop-blur-sm">
      <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        <Link href="/" className="text-lg font-bold text-[var(--color-text)]">
          AI Agents From Scratch
        </Link>

        {/* Desktop nav */}
        <nav className="hidden items-center gap-6 md:flex">
          {NAV_ITEMS.map((item) => {
            const isActive = pathname.startsWith(item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "text-sm font-medium transition-colors hover:text-zinc-900 dark:hover:text-white",
                  isActive
                    ? "text-zinc-900 dark:text-white"
                    : "text-zinc-500 dark:text-zinc-400"
                )}
              >
                {item.label}
              </Link>
            );
          })}

          <button
            onClick={toggleDark}
            className="rounded-md p-1.5 text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-white"
            aria-label="Toggle dark mode"
          >
            {mounted ? (
              dark ? (
                <Sun size={16} />
              ) : (
                <Moon size={16} />
              )
            ) : (
              <span className="inline-block h-4 w-4" />
            )}
          </button>

          <a
            href="https://github.com/pguso/agents-from-scratch"
            target="_blank"
            rel="noopener noreferrer"
            className="text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-white"
            aria-label="GitHub repository"
          >
            <Github size={18} />
          </a>
        </nav>

        {/* Mobile hamburger */}
        <button
          onClick={() => setMobileOpen(!mobileOpen)}
          className="flex min-h-[44px] min-w-[44px] items-center justify-center md:hidden"
          aria-label="Toggle menu"
        >
          {mobileOpen ? <X size={20} /> : <Menu size={20} />}
        </button>
      </div>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="border-t border-[var(--color-border)] bg-[var(--color-bg)] p-4 md:hidden">
          {NAV_ITEMS.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex min-h-[44px] items-center text-sm",
                pathname.startsWith(item.href)
                  ? "font-medium text-[var(--color-text)]"
                  : "text-[var(--color-text-secondary)]"
              )}
              onClick={() => setMobileOpen(false)}
            >
              {item.label}
            </Link>
          ))}
          <div className="mt-3 flex items-center justify-between border-t border-[var(--color-border)] pt-3">
            <button
              onClick={toggleDark}
              className="flex min-h-[44px] min-w-[44px] items-center justify-center rounded-md text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-white"
              aria-label="Toggle dark mode"
            >
              {mounted ? (
                dark ? (
                  <Sun size={18} />
                ) : (
                  <Moon size={18} />
                )
              ) : (
                <span className="inline-block h-[18px] w-[18px]" />
              )}
            </button>
            <a
              href="https://github.com/pguso/agents-from-scratch"
              target="_blank"
              rel="noopener noreferrer"
              className="flex min-h-[44px] min-w-[44px] items-center justify-center text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-white"
              aria-label="GitHub repository"
            >
              <Github size={18} />
            </a>
          </div>
        </div>
      )}
    </header>
  );
}
