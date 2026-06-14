export type ChapterLayer =
  | "fundamentals"
  | "agent-core"
  | "memory"
  | "reasoning"
  | "advanced";

export interface ChapterImage {
  src: string;
  alt: string;
}

export interface ChapterVersion {
  id: string;
  dirName: string;
  title: string;
  subtitle: string;
  description: string;
  loc: number;
  tools: string[];
  newTools: string[];
  classes: { name: string; startLine: number; endLine: number }[];
  functions: { name: string; signature: string; startLine: number }[];
  layer: ChapterLayer;
  source: string;
  filename: string;
  hasCode: boolean;
  hasConcepts: boolean;
  images: ChapterImage[];
}

export interface ChapterDiff {
  from: string;
  to: string;
  newClasses: string[];
  newFunctions: string[];
  newTools: string[];
  locDelta: number;
}

export interface ChapterDoc {
  chapter: string;
  type: "code" | "concept";
  title: string;
  content: string;
}

export interface ChapterIndex {
  chapters: ChapterVersion[];
  diffs: ChapterDiff[];
}
