import * as fs from "fs";
import * as path from "path";
import type {
  ChapterVersion,
  ChapterDiff,
  ChapterDoc,
  ChapterIndex,
  ChapterImage,
} from "../src/types/chapter-data";
import { CHAPTER_ORDER, CHAPTER_META } from "../src/lib/constants";

const WEB_DIR = path.resolve(__dirname, "..");
const REPO_ROOT = path.resolve(WEB_DIR, "..");
const EXAMPLES_DIR = path.join(REPO_ROOT, "examples");
const SRC_DIR = path.join(REPO_ROOT, "src");
const CORE_DIR = path.join(SRC_DIR, "AiAgents.Core");
const DIAGRAMS_DIR = path.join(REPO_ROOT, "diagrams");
const OUT_DIR = path.join(WEB_DIR, "src", "data", "generated");
const PUBLIC_DIR = path.join(WEB_DIR, "public");
const COURSE_ASSETS_DIR = path.join(PUBLIC_DIR, "course-assets");

interface ChapterSource {
  id: string;
  dirName: string;
  dirPath: string;
  codePath: string | null;
}

function listExamples(): ChapterSource[] {
  if (!fs.existsSync(EXAMPLES_DIR)) return [];

  return fs
    .readdirSync(EXAMPLES_DIR, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => entry.name)
    .filter((name) => /^\d{2}_/.test(name))
    .sort()
    .map((dirName) => {
      const id = dirName.substring(0, 2);
      const chapterDir = path.join(SRC_DIR, `Chapter${id}`);
      const codePath = path.join(chapterDir, "Program.cs");
      return {
        id,
        dirName,
        dirPath: path.join(EXAMPLES_DIR, dirName),
        codePath: fs.existsSync(codePath) ? codePath : null,
      };
    });
}

function walkCsFiles(dir: string): string[] {
  const out: string[] = [];
  if (!fs.existsSync(dir)) return out;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (entry.name === "bin" || entry.name === "obj") continue;
      out.push(...walkCsFiles(p));
    } else if (entry.isFile() && p.endsWith(".cs")) {
      out.push(p);
    }
  }
  return out;
}

function extractClasses(
  lines: string[]
): { name: string; startLine: number; endLine: number }[] {
  const classes: { name: string; startLine: number; endLine: number }[] = [];
  const classPattern =
    /^\s*(?:public|internal|private|protected|file|static|sealed|abstract|partial|new|virtual|override|unsafe|readonly|ref|\s+)*\b(?:class|record|struct|interface|enum)\s+(\w+)/;

  for (let i = 0; i < lines.length; i++) {
    const match = lines[i].match(classPattern);
    if (!match) continue;
    const name = match[1];
    const startLine = i + 1;
    let endLine = lines.length;
    for (let j = i + 1; j < lines.length; j++) {
      if (classPattern.test(lines[j])) {
        endLine = j;
        break;
      }
    }
    classes.push({ name, startLine, endLine });
  }
  return classes;
}

function extractFunctions(
  lines: string[]
): { name: string; signature: string; startLine: number }[] {
  const functions: { name: string; signature: string; startLine: number }[] = [];
  const skip = new Set([
    "if", "else", "for", "foreach", "while", "do", "switch", "try", "catch", "finally",
    "using", "return", "throw", "yield", "new", "var", "namespace", "class", "struct",
    "interface", "enum", "record", "public", "private", "internal", "protected", "static",
    "sealed", "abstract", "partial", "override", "virtual", "async", "extern", "unsafe",
    "readonly", "volatile", "fixed", "required", "file", "out", "ref", "in", "params",
    "this", "typeof", "sizeof", "nameof", "is", "as", "default", "true", "false", "null",
    "where", "get", "set", "init", "add", "remove", "case", "break", "continue",
  ]);
  const statementPrefixes = [
    "var ", "let ", "const ", "new ", "await ", "return ", "throw ", "yield ",
    "= ", "+=", "-=", "*=", "/=", "?: ", "?.", "?.(",
  ];

  let i = 0;
  while (i < lines.length) {
    const trimmed = lines[i].trim();
    if (
      !trimmed ||
      trimmed.startsWith("//") ||
      trimmed.startsWith("/*") ||
      trimmed.startsWith("*")
    ) {
      i++;
      continue;
    }

    if (statementPrefixes.some((p) => trimmed.startsWith(p))) {
      i++;
      continue;
    }

    const openParen = trimmed.indexOf("(");
    if (openParen < 0) {
      i++;
      continue;
    }

    let combined = trimmed;
    let j = i;
    if (trimmed.indexOf(")", openParen) < 0) {
      while (
        j + 1 < lines.length &&
        combined.indexOf(")", openParen) < 0 &&
        j - i < 6
      ) {
        j++;
        combined += " " + lines[j].trim();
      }
    }

    const closeParen = combined.indexOf(")", openParen);
    if (closeParen < 0) {
      i = Math.max(i + 1, j + 1);
      continue;
    }

    const afterParen = combined.substring(closeParen + 1).trimStart();
    let looksLikeMethod =
      afterParen.startsWith("{") ||
      /^=>/.test(afterParen) ||
      /^\s*where\b/.test(afterParen);

    if (!looksLikeMethod && afterParen === "" && j + 1 < lines.length) {
      let k = j + 1;
      while (k < lines.length && lines[k].trim() === "") k++;
      if (k < lines.length) {
        const next = lines[k].trim();
        if (
          next.startsWith("{") ||
          next.startsWith("=>") ||
          /^\s*where\b/.test(next)
        ) {
          looksLikeMethod = true;
        }
      }
    }

    if (!looksLikeMethod) {
      i = Math.max(i + 1, j + 1);
      continue;
    }

    const before = combined.substring(0, openParen).trim();
    if (/\bnew\s+\w*$/.test(before)) {
      i = Math.max(i + 1, j + 1);
      continue;
    }

    const beforeNoGeneric = before.replace(/<[^>]+>$/, "").trim();
    const tokens = beforeNoGeneric.split(/\s+/);
    const name = tokens[tokens.length - 1];
    if (!name || !/^\w+$/.test(name)) {
      i = Math.max(i + 1, j + 1);
      continue;
    }
    if (skip.has(name)) {
      i = Math.max(i + 1, j + 1);
      continue;
    }

    const signature = combined
      .split(/\s*\{/)[0]
      .split(/\s*=>/)[0]
      .trim();

    functions.push({ name, signature, startLine: i + 1 });
    i = Math.max(i + 1, j + 1);
  }
  return functions;
}

function extractTools(source: string): string[] {
  const tools = new Set<string>();

  // new AgentTool(name: "...", ...) or new AgentTool("...", ...)
  const agentToolNamePattern = /new\s+AgentTool\s*\(\s*(?:name\s*:\s*)?"([^"]+)"/g;
  let m: RegExpExecArray | null;
  while ((m = agentToolNamePattern.exec(source)) !== null) {
    tools.add(m[1]);
  }

  // ChatTool.CreateFunctionTool("...", ...) or ChatTool.CreateFunctionTool(key: "...", ...)
  const chatToolPattern = /CreateFunctionTool\s*\(\s*(?:key\s*:\s*)?"([^"]+)"/g;
  while ((m = chatToolPattern.exec(source)) !== null) {
    tools.add(m[1]);
  }

  // tools.Register("...", ...)
  const registerPattern = /tools\.Register\s*\(\s*"([^"]+)"/g;
  while ((m = registerPattern.exec(source)) !== null) {
    tools.add(m[1]);
  }

  return Array.from(tools);
}

function countLoc(lines: string[]): number {
  let inBlockComment = false;
  return lines.filter((line) => {
    const trimmed = line.trim();
    if (trimmed === "") return false;

    if (inBlockComment) {
      if (trimmed.includes("*/")) inBlockComment = false;
      return false;
    }
    if (trimmed.startsWith("/*")) {
      if (!trimmed.endsWith("*/") && !trimmed.includes("*/")) inBlockComment = true;
      return false;
    }
    if (trimmed.startsWith("//")) return false;
    if (trimmed.startsWith("*")) return false;
    return true;
  }).length;
}

function extractSourceTools(source: string): string[] {
  const allTools = new Set<string>();
  for (const name of extractTools(source)) {
    allTools.add(name);
  }
  return Array.from(allTools);
}

function cleanCourseAssets() {
  fs.rmSync(COURSE_ASSETS_DIR, { recursive: true, force: true });
  fs.mkdirSync(COURSE_ASSETS_DIR, { recursive: true });
}

function copyDiagrams(): ChapterImage[] {
  if (!fs.existsSync(DIAGRAMS_DIR)) return [];

  const outDir = path.join(COURSE_ASSETS_DIR, "diagrams");
  fs.mkdirSync(outDir, { recursive: true });
  fs.cpSync(DIAGRAMS_DIR, outDir, { recursive: true });

  return fs
    .readdirSync(DIAGRAMS_DIR)
    .filter((filename) => /\.(png|svg|jpg|jpeg|gif|webp)$/i.test(filename))
    .sort()
    .map((filename) => ({
      src: `/course-assets/diagrams/${filename}`,
      alt: filename.replace(/\.(png|svg|jpg|jpeg|gif|webp)$/i, "").replace(/-/g, " "),
    }));
}

function copyChapterImages(chapter: ChapterSource): ChapterImage[] {
  const imagesDir = path.join(chapter.dirPath, "images");
  if (!fs.existsSync(imagesDir)) return [];

  const outDir = path.join(COURSE_ASSETS_DIR, chapter.dirName);
  fs.mkdirSync(outDir, { recursive: true });
  fs.cpSync(imagesDir, outDir, { recursive: true });

  return fs
    .readdirSync(imagesDir)
    .filter((filename) => /\.(png|svg|jpg|jpeg|gif|webp)$/i.test(filename))
    .sort()
    .map((filename) => ({
      src: `/course-assets/${chapter.dirName}/${filename}`,
      alt: filename.replace(/\.(png|svg|jpg|jpeg|gif|webp)$/i, "").replace(/-/g, " "),
    }));
}

function rewriteMarkdown(content: string, chapter: ChapterSource): string {
  let next = content;

  // Rewrite diagrams/ references
  next = next.replace(
    /(!\[[^\]]*\]\()diagrams\/([^)]+)(\))/g,
    `$1/course-assets/diagrams/$2$3`
  );

  // Rewrite examples/NN_name/images/ references
  next = next.replace(
    /(!\[[^\]]*\]\()images\/([^)]+)(\))/g,
    `$1/course-assets/${chapter.dirName}/$2$3`
  );

  return next;
}

function titleFromMarkdown(content: string, fallback: string): string {
  const titleMatch = content.match(/^#\s+(.+)$/m);
  return titleMatch ? titleMatch[1] : fallback;
}

function buildVersions(chapters: ChapterSource[]): ChapterVersion[] {
  const diagramImages = copyDiagrams();

  return chapters.map((chapter) => {
    const meta = CHAPTER_META[chapter.id];
    const chapterImages = copyChapterImages(chapter);

    if (!chapter.codePath) {
      return {
        id: chapter.id,
        dirName: chapter.dirName,
        title: meta?.title ?? `Chapter ${chapter.id}`,
        subtitle: meta?.subtitle ?? "",
        description: meta?.description ?? "",
        loc: 0,
        tools: [],
        newTools: [],
        classes: [],
        functions: [],
        layer: meta?.layer ?? "fundamentals",
        source: "",
        filename: "",
        hasCode: false,
        hasConcepts: false,
        images: [...diagramImages, ...chapterImages],
      };
    }

    const source = fs.readFileSync(chapter.codePath, "utf-8");
    const lines = source.split("\n");

    return {
      id: chapter.id,
      dirName: chapter.dirName,
      title: meta?.title ?? `Chapter ${chapter.id}`,
      subtitle: meta?.subtitle ?? "",
      description: meta?.description ?? "",
      loc: countLoc(lines),
      tools: extractSourceTools(source),
      newTools: [],
      classes: extractClasses(lines),
      functions: extractFunctions(lines),
      layer: meta?.layer ?? "fundamentals",
      source,
      filename: `src/Chapter${chapter.id}/Program.cs`,
      hasCode: true,
      hasConcepts: false,
      images: [...diagramImages, ...chapterImages],
    };
  });
}

function buildDocs(chapters: ChapterSource[]): ChapterDoc[] {
  const docs: ChapterDoc[] = [];

  for (const chapter of chapters) {
    const codePath = path.join(chapter.dirPath, "CODE.md");
    const conceptPath = path.join(chapter.dirPath, "CONCEPT.md");

    if (fs.existsSync(codePath)) {
      const raw = fs.readFileSync(codePath, "utf-8");
      docs.push({
        chapter: chapter.id,
        type: "code",
        title: titleFromMarkdown(raw, "Code Explanation"),
        content: rewriteMarkdown(raw, chapter),
      });
    }

    if (fs.existsSync(conceptPath)) {
      const raw = fs.readFileSync(conceptPath, "utf-8");
      docs.push({
        chapter: chapter.id,
        type: "concept",
        title: titleFromMarkdown(raw, "Concepts"),
        content: rewriteMarkdown(raw, chapter),
      });
    }
  }

  return docs;
}

function computeNewTools(chapters: ChapterVersion[]) {
  for (let i = 0; i < chapters.length; i++) {
    const prev = i > 0 ? new Set(chapters[i - 1].tools) : new Set<string>();
    chapters[i].newTools = chapters[i].tools.filter((tool) => !prev.has(tool));
  }
}

function buildDiffs(chapters: ChapterVersion[]): ChapterDiff[] {
  const diffs: ChapterDiff[] = [];
  const chapterMap = new Map(chapters.map((c) => [c.id, c]));

  for (let i = 1; i < CHAPTER_ORDER.length; i++) {
    const fromId = CHAPTER_ORDER[i - 1];
    const toId = CHAPTER_ORDER[i];
    const fromChapter = chapterMap.get(fromId);
    const toChapter = chapterMap.get(toId);
    if (!fromChapter || !toChapter) continue;

    const fromClassNames = new Set(fromChapter.classes.map((c) => c.name));
    const fromFuncNames = new Set(fromChapter.functions.map((f) => f.name));
    const fromToolNames = new Set(fromChapter.tools);

    diffs.push({
      from: fromId,
      to: toId,
      newClasses: toChapter.classes
        .map((c) => c.name)
        .filter((name) => !fromClassNames.has(name)),
      newFunctions: toChapter.functions
        .map((f) => f.name)
        .filter((name) => !fromFuncNames.has(name)),
      newTools: toChapter.tools.filter((tool) => !fromToolNames.has(tool)),
      locDelta: toChapter.loc - fromChapter.loc,
    });
  }

  return diffs;
}

function main() {
  console.log("Extracting course content...");
  console.log(`  Repo root: ${REPO_ROOT}`);

  cleanCourseAssets();

  const chapters = listExamples();
  console.log(`  Found ${chapters.length} chapters in examples/`);

  const versions = buildVersions(chapters);
  const docs = buildDocs(chapters);

  // Mark chapters that have concept docs
  const hasConceptDoc = new Set(docs.filter((d) => d.type === "concept").map((d) => d.chapter));
  for (const v of versions) {
    v.hasConcepts = hasConceptDoc.has(v.id);
  }

  computeNewTools(versions);
  const diffs = buildDiffs(versions);

  fs.mkdirSync(OUT_DIR, { recursive: true });

  const index: ChapterIndex = { chapters: versions, diffs };
  fs.writeFileSync(path.join(OUT_DIR, "chapters.json"), JSON.stringify(index, null, 2));
  fs.writeFileSync(path.join(OUT_DIR, "docs.json"), JSON.stringify(docs, null, 2));

  console.log("\nExtraction complete:");
  console.log(`  ${versions.length} chapters`);
  console.log(`  ${diffs.length} diffs`);
  console.log(`  ${docs.length} docs`);
  for (const v of versions) {
    console.log(
      `    ${v.id}: ${v.loc} LOC, ${v.tools.length} tools, ${v.classes.length} classes, ${v.functions.length} functions`
    );
  }
}

main();
