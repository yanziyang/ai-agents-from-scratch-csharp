import { Timeline } from "@/components/timeline/timeline";

export default function TimelinePage() {
  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold">Learning Path</h1>
        <p className="mt-2 text-[var(--color-text-secondary)]">
          15 chapters from a basic LLM call to advanced agent patterns
        </p>
      </div>
      <Timeline />
    </div>
  );
}
