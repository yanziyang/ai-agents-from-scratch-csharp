import type { ChapterLayer } from "@/types/chapter-data";

export const CHAPTER_ORDER = [
  "01",
  "02",
  "03",
  "04",
  "05",
  "06",
  "07",
  "08",
  "09",
  "10",
  "11",
  "12",
  "13",
  "14",
  "15",
] as const;

export type ChapterId = (typeof CHAPTER_ORDER)[number];

export interface ChapterMeta {
  title: string;
  subtitle: string;
  description: string;
  layer: ChapterLayer;
  prevChapter: ChapterId | null;
}

export const CHAPTER_META: Record<string, ChapterMeta> = {
  "01": {
    title: "Basic LLM Interaction",
    subtitle: "The simplest possible call to a hosted model",
    description:
      "Learn how to call a hosted LLM from C#, send a message, and print the response.",
    layer: "fundamentals",
    prevChapter: null,
  },
  "02": {
    title: "Using the OpenAI .NET SDK with DeepSeek",
    subtitle: "Point the OpenAI SDK at DeepSeek's compatible endpoint",
    description:
      "Configure the official OpenAI .NET SDK to talk to DeepSeek, and understand temperature and token usage.",
    layer: "fundamentals",
    prevChapter: "01",
  },
  "03": {
    title: "System Prompts & Specialization",
    subtitle: "Give the model a role and output format",
    description:
      "Use system prompts to specialize behavior, control output format, and apply role-based constraints.",
    layer: "fundamentals",
    prevChapter: "02",
  },
  "04": {
    title: "Reasoning & Problem Solving",
    subtitle: "Configure the model for logical reasoning",
    description:
      "Explore complex quantitative problems, the limits of pure LLM reasoning, and when to reach for external tools.",
    layer: "agent-core",
    prevChapter: "03",
  },
  "05": {
    title: "Parallel Processing",
    subtitle: "Run multiple requests concurrently",
    description:
      "Process multiple requests in parallel with async/await patterns and optimize throughput.",
    layer: "agent-core",
    prevChapter: "04",
  },
  "06": {
    title: "Streaming & Response Control",
    subtitle: "Stream tokens as they are generated",
    description:
      "Handle streaming responses, manage token budgets, and build progressive output displays.",
    layer: "agent-core",
    prevChapter: "05",
  },
  "07": {
    title: "Function Calling (Tools)",
    subtitle: "From text generator to agent",
    description:
      "Define tools the LLM can invoke, describe parameters with JSON Schema, and close the loop with tool results.",
    layer: "agent-core",
    prevChapter: "06",
  },
  "08": {
    title: "Persistent State",
    subtitle: "Remember facts across sessions",
    description:
      "Persist information across sessions with long-term memory, facts, preferences, and retrieval strategies.",
    layer: "memory",
    prevChapter: "07",
  },
  "09": {
    title: "Reasoning + Acting",
    subtitle: "The ReAct pattern",
    description:
      "Build iterative Reason → Act → Observe loops for multi-step problem solving and self-correction.",
    layer: "reasoning",
    prevChapter: "08",
  },
  "10": {
    title: "Atom of Thought Planning",
    subtitle: "Plan with atomic, dependent operations",
    description:
      "Generate structured reasoning plans with dependencies, then execute them deterministically.",
    layer: "reasoning",
    prevChapter: "09",
  },
  "11": {
    title: "Resilience for LLM + Tools",
    subtitle: "Errors are the start of a retry",
    description:
      "Classify errors, apply retries with backoff and jitter, and gracefully degrade when the LLM path fails.",
    layer: "reasoning",
    prevChapter: "10",
  },
  "12": {
    title: "Search over Reasoning Branches",
    subtitle: "Tree of Thought",
    description:
      "Generate candidate hypotheses, rank and prune branches with verifiable scores, and search toward a solution.",
    layer: "reasoning",
    prevChapter: "11",
  },
  "13": {
    title: "DAG Merge for Multi-Source Outputs",
    subtitle: "Graph of Thought",
    description:
      "Model reasoning as a graph: parallel hypotheses, contrast, refine, aggregate, and conclude.",
    layer: "reasoning",
    prevChapter: "12",
  },
  "14": {
    title: "Auditable Stepwise Decisions",
    subtitle: "Chain of Thought",
    description:
      "Split high-stakes decisions into explicit phases, extract facts first, and produce auditable outputs.",
    layer: "reasoning",
    prevChapter: "13",
  },
  "15": {
    title: "Tool Routing with Embeddings",
    subtitle: "Narrow the tool catalog per request",
    description:
      "Use local embeddings to score user intent against tool exemplars and pass only the top-k tools to the model.",
    layer: "advanced",
    prevChapter: "14",
  },
};

export const LAYERS: {
  id: ChapterLayer;
  label: string;
  color: string;
  chapters: ChapterId[];
}[] = [
  {
    id: "fundamentals",
    label: "Fundamentals",
    color: "#3B82F6",
    chapters: ["01", "02", "03"],
  },
  {
    id: "agent-core",
    label: "Agent Core",
    color: "#10B981",
    chapters: ["04", "05", "06", "07"],
  },
  {
    id: "memory",
    label: "Memory",
    color: "#8B5CF6",
    chapters: ["08"],
  },
  {
    id: "reasoning",
    label: "Reasoning Patterns",
    color: "#F59E0B",
    chapters: ["09", "10", "11", "12", "13", "14"],
  },
  {
    id: "advanced",
    label: "Advanced",
    color: "#EF4444",
    chapters: ["15"],
  },
];
