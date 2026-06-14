using System.Text.Json;

namespace AiAgents.Core.Visualization;

public static class VisualizationWriters
{
    public static void WriteTreeOfThoughtVisualization(string outputDir, List<ScoredHypothesis> scored, string winnerName, object analysis)
    {
        Directory.CreateDirectory(outputDir);

        var treeNodes = new List<TreeNode>
        {
            new TreeNode(0, null, 0, "Behavior input", 0, true)
        };

        for (int i = 0; i < scored.Count; i++)
        {
            treeNodes.Add(new TreeNode(
                i + 1,
                0,
                1,
                scored[i].Hypothesis.Name,
                scored[i].Score,
                scored[i].Hypothesis.Name == winnerName));
        }

        treeNodes.Add(new TreeNode(
            scored.Count + 1,
            scored.FindIndex(s => s.Hypothesis.Name == winnerName) + 1,
            2,
            "Conclusion from winner",
            scored.Find(s => s.Hypothesis.Name == winnerName)?.Score ?? 0,
            true));

        var winningIds = new HashSet<int>(treeNodes
            .Where(n => n.Thought == winnerName || n.Thought == "Conclusion from winner" || n.Id == 0)
            .Select(n => n.Id));

        var nodes = treeNodes.Select(n => new Dictionary<string, object?>
        {
            ["id"] = n.Id,
            ["parentId"] = n.ParentId,
            ["depth"] = n.Depth,
            ["thought"] = n.Thought,
            ["lowerBound"] = n.LowerBound,
            ["kept"] = n.Kept,
            ["winning"] = winningIds.Contains(n.Id)
        }).ToList();

        var data = JsonSerializer.Serialize(new { nodes, analysis, winnerName });
        var html = TreeTemplate.Replace("__DATA__", data);
        File.WriteAllText(Path.Combine(outputDir, "visualization.html"), html);
        Console.WriteLine($"\nVisualization written -> {Path.Combine(outputDir, "visualization.html")}");
    }

    private record TreeNode(int Id, int? ParentId, int Depth, string Thought, double LowerBound, bool Kept);

    private const string TreeTemplate = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<title>Tree of Thought — Motivation Analysis</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: system-ui, -apple-system, sans-serif; background: #0f1117; color: #e2e8f0; }
  header { padding: 22px 32px 14px; border-bottom: 1px solid #1e2535; }
  header h1 { font-size: 1.28rem; font-weight: 700; color: #f7fafc; }
  header p { margin-top: 4px; font-size: .84rem; color: #a0aec0; }
  .topbar { display: flex; justify-content: space-between; align-items: center; padding: 10px 32px; background: #141820; border-bottom: 1px solid #1e2535; flex-wrap: wrap; gap: 12px; }
  .legend { display: flex; gap: 14px; font-size: .75rem; color: #94a3b8; }
  .leg-item { display: flex; align-items: center; gap: 6px; }
  .leg-dot { width: 10px; height: 10px; border-radius: 50%; border: 1px solid; }
  #canvas-wrap { position: relative; overflow-x: auto; padding: 26px 32px 18px; }
  #canvas { position: relative; min-height: 350px; min-width: 920px; }
  #lines { position: absolute; inset: 0; pointer-events: none; }
  .stage { position: absolute; top: 0; font-size: .69rem; letter-spacing: .08em; text-transform: uppercase; color: #64748b; font-weight: 700; }
  .node { position: absolute; width: 220px; border-radius: 10px; padding: 11px 12px 10px; border: 1.5px solid #2d3748; background: #161c27; }
  .node.root { background: #172033; border-color: #3b82f6; }
  .node.kept { background: #0f1e14; border-color: #2f7a50; }
  .node.win { background: #1e1600; border-color: #d4a017; border-width: 2px; }
  .node.pruned { opacity: .48; }
  .title { font-size: .79rem; line-height: 1.42; color: #e2e8f0; }
  .score { margin-top: 8px; display: inline-block; font-size: .66rem; color: #94a3b8; border: 1px solid #334155; border-radius: 999px; padding: 2px 7px; }
  #analysis { margin: 0 32px 28px; padding: 18px 22px; border-radius: 10px; border: 1px solid #1e2535; background: #141820; }
  #analysis h2 { font-size: .73rem; letter-spacing: .08em; text-transform: uppercase; color: #64748b; margin-bottom: 10px; }
  #summary { color: #cbd5e1; font-size: .9rem; line-height: 1.65; }
</style>
</head>
<body>
<header>
  <h1>Tree of Thought — Motivation Analysis</h1>
  <p>Branch hypotheses, score each branch, prune, conclude from one winner.</p>
</header>

<div class="topbar">
  <div class="legend">
    <span class="leg-item"><span class="leg-dot" style="background:#172033;border-color:#3b82f6"></span>Behavior input</span>
    <span class="leg-item"><span class="leg-dot" style="background:#0f1e14;border-color:#2f7a50"></span>Kept branch</span>
    <span class="leg-item"><span class="leg-dot" style="background:#1e1600;border-color:#d4a017"></span>Winning branch</span>
    <span class="leg-item"><span class="leg-dot" style="background:#161c27;border-color:#2d3748"></span>Pruned branch</span>
  </div>
</div>

<div id="canvas-wrap">
  <div id="canvas">
    <svg id="lines"></svg>
  </div>
</div>

<div id="analysis">
  <h2>Winner Summary</h2>
  <p id="summary"></p>
</div>

<script>
const D = __DATA__;
const nodes = D.nodes;
const canvas = document.getElementById("canvas");
const svg = document.getElementById("lines");

const byDepth = {};
for (const n of nodes) {
  if (!byDepth[n.depth]) byDepth[n.depth] = [];
  byDepth[n.depth].push(n);
}

const COL_X = { 0: 30, 1: 340, 2: 680 };
const ROW_GAP = 112;
const NODE_W = 220;
const NODE_H = 78;

const stageNames = {
  0: "Behavior",
  1: "Phase 1-3: Branch / Score / Prune",
  2: "Phase 4: Conclusion"
};

for (const [depth, label] of Object.entries(stageNames)) {
  const el = document.createElement("div");
  el.className = "stage";
  el.style.left = COL_X[depth] + "px";
  el.textContent = label;
  canvas.appendChild(el);
}

const pos = {};
for (const depthKey of Object.keys(byDepth)) {
  const depth = Number(depthKey);
  const list = byDepth[depth];
  list.forEach((n, i) => {
    pos[n.id] = { x: COL_X[depth], y: 36 + i * ROW_GAP };
  });
}

const maxY = Math.max(...Object.values(pos).map(p => p.y));
canvas.style.height = (maxY + NODE_H + 40) + "px";
svg.setAttribute("width", canvas.clientWidth);
svg.setAttribute("height", canvas.clientHeight);

for (const n of nodes) {
  const card = document.createElement("div");
  let cls = "node";
  if (n.depth === 0) cls += " root";
  else if (n.winning) cls += " win";
  else if (n.kept) cls += " kept";
  else cls += " pruned";
  card.className = cls;
  card.style.left = pos[n.id].x + "px";
  card.style.top = pos[n.id].y + "px";

  const title = document.createElement("div");
  title.className = "title";
  title.textContent = n.thought;
  card.appendChild(title);

  if (n.depth > 0) {
    const score = document.createElement("div");
    score.className = "score";
    score.textContent = "Score: " + n.lowerBound;
    card.appendChild(score);
  }
  canvas.appendChild(card);
}

function drawLine(parent, child, color, width, dashed = false, opacity = 1) {
  const x1 = pos[parent.id].x + NODE_W;
  const y1 = pos[parent.id].y + NODE_H / 2;
  const x2 = pos[child.id].x;
  const y2 = pos[child.id].y + NODE_H / 2;
  const cx = (x1 + x2) / 2;
  const p = document.createElementNS("http://www.w3.org/2000/svg", "path");
  p.setAttribute("d", "M" + x1 + "," + y1 + " C" + cx + "," + y1 + " " + cx + "," + y2 + " " + x2 + "," + y2);
  p.setAttribute("fill", "none");
  p.setAttribute("stroke", color);
  p.setAttribute("stroke-width", width);
  p.setAttribute("opacity", opacity);
  if (dashed) p.setAttribute("stroke-dasharray", "6,4");
  svg.appendChild(p);
}

for (const n of nodes) {
  if (n.parentId == null) continue;
  const p = nodes.find(x => x.id === n.parentId);
  const winningEdge = n.winning && p && p.winning;
  drawLine(p, n, winningEdge ? "#d4a017" : (n.kept ? "#2f7a50" : "#334155"), winningEdge ? 2.4 : 1.4, !n.kept, n.kept ? 1 : 0.35);
}

document.getElementById("summary").textContent = D.analysis.summary || "";
</script>
</body>
</html>
""";

    public record Hypothesis(string Name, string Argument, List<string> Signals, List<string> CounterEvidence);
    public record ScoredHypothesis(Hypothesis Hypothesis, double Score, string Reasoning, string BlindSpot, ScoreDetails Details);
    public record ScoreDetails(double ExplanatoryPower, double Plausibility, double Falsifiability);

    public record GraphNode(string Id, string Type, string Label, double? Score = null);
    public record GraphEdge(string From, string To);
    public record CoTPhase(string Name, string Status, string Summary, double? Score = null);
    public record ToolRoutingLogEntry(string Label, string Prompt, Dictionary<string, double> Scores, List<string> SelectedKeys, string Answer);

    public static void WriteToolRoutingVisualization(string outputDir, List<ToolRoutingLogEntry> logs)
    {
        Directory.CreateDirectory(outputDir);

        var data = JsonSerializer.Serialize(logs);
        var html = ToolRoutingTemplate.Replace("__DATA__", data);
        File.WriteAllText(Path.Combine(outputDir, "visualization.html"), html);
        Console.WriteLine($"\nVisualization written -> {Path.Combine(outputDir, "visualization.html")}");
    }

    public static void WriteChainOfThoughtVisualization(string outputDir, List<CoTPhase> phases, object decision)
    {
        Directory.CreateDirectory(outputDir);

        var data = JsonSerializer.Serialize(new { phases, decision });
        var html = CoTTemplate.Replace("__DATA__", data);
        File.WriteAllText(Path.Combine(outputDir, "visualization.html"), html);
        Console.WriteLine($"\nVisualization written -> {Path.Combine(outputDir, "visualization.html")}");
    }

    public static void WriteGraphOfThoughtVisualization(string outputDir, List<GraphNode> graphNodes, List<GraphEdge> graphEdges, object analysis)
    {
        Directory.CreateDirectory(outputDir);

        var nodes = graphNodes.Select(n => new Dictionary<string, object?>
        {
            ["id"] = n.Id,
            ["type"] = n.Type,
            ["label"] = n.Label,
            ["score"] = n.Score
        }).ToList();

        var edges = graphEdges.Select(e => new Dictionary<string, object?>
        {
            ["from"] = e.From,
            ["to"] = e.To
        }).ToList();

        var data = JsonSerializer.Serialize(new { nodes, edges, analysis });
        var html = GraphTemplate.Replace("__DATA__", data);
        File.WriteAllText(Path.Combine(outputDir, "visualization.html"), html);
        Console.WriteLine($"\nVisualization written -> {Path.Combine(outputDir, "visualization.html")}");
    }

    private const string GraphTemplate = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<title>Graph of Thought — Motivation Analysis</title>
<script type="text/javascript" src="https://unpkg.com/vis-network/standalone/umd/vis-network.min.js"></script>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: system-ui, -apple-system, sans-serif; background: #0f1117; color: #e2e8f0; }
  header { padding: 22px 32px 14px; border-bottom: 1px solid #1e2535; }
  header h1 { font-size: 1.28rem; font-weight: 700; color: #f7fafc; }
  header p { margin-top: 4px; font-size: .84rem; color: #a0aec0; }
  .topbar { display: flex; justify-content: space-between; align-items: center; padding: 10px 32px; background: #141820; border-bottom: 1px solid #1e2535; flex-wrap: wrap; gap: 12px; }
  .legend { display: flex; gap: 14px; font-size: .75rem; color: #94a3b8; flex-wrap: wrap; }
  .leg-item { display: flex; align-items: center; gap: 6px; }
  .leg-dot { width: 12px; height: 12px; border-radius: 50%; border: 1px solid #ffffff30; }
  #graph { height: 520px; margin: 18px 32px 0; border: 1px solid #1e2535; border-radius: 10px; background: #141820; }
  #analysis { margin: 18px 32px 28px; padding: 18px 22px; border-radius: 10px; border: 1px solid #1e2535; background: #141820; }
  #analysis h2 { font-size: .73rem; letter-spacing: .08em; text-transform: uppercase; color: #64748b; margin-bottom: 10px; }
  #summary { color: #cbd5e1; font-size: .9rem; line-height: 1.65; }
</style>
</head>
<body>
<header>
  <h1>Graph of Thought — Motivation Analysis</h1>
  <p>Hypotheses stay alive, get contrasted, refined, aggregated, and merged into one conclusion.</p>
</header>

<div class="topbar">
  <div class="legend" id="legend"></div>
</div>

<div id="graph"></div>

<div id="analysis">
  <h2>Integrated Conclusion</h2>
  <p id="summary"></p>
</div>

<script>
const D = __DATA__;
const palette = {
  root: { background: "#172033", border: "#3b82f6", color: "#e2e8f0" },
  hypothesis: { background: "#1e293b", border: "#60a5fa", color: "#e2e8f0" },
  contrast: { background: "#331c1c", border: "#f87171", color: "#fecaca" },
  refined: { background: "#1c2e1c", border: "#4ade80", color: "#dcfce7" },
  synthesis: { background: "#2e1c2e", border: "#c084fc", color: "#f3e8ff" },
  conclusion: { background: "#2a1d00", border: "#fbbf24", color: "#fef3c7" }
};

const visNodes = new vis.DataSet(D.nodes.map(n => ({
  id: n.id,
  label: n.score != null ? n.label + "\\n(score " + n.score + ")" : n.label,
  group: n.type,
  color: palette[n.type] || palette.hypothesis,
  font: { color: (palette[n.type] || palette.hypothesis).color, size: 13, multi: "html" },
  shape: n.type === "conclusion" ? "box" : "dot",
  size: n.type === "root" ? 22 : (n.type === "conclusion" ? 0 : 16),
  borderWidth: 2
})));

const visEdges = new vis.DataSet(D.edges.map(e => ({
  from: e.from,
  to: e.to,
  arrows: "to",
  color: { color: "#475569", highlight: "#94a3b8" },
  width: 1.5
})));

new vis.Network(document.getElementById("graph"), { nodes: visNodes, edges: visEdges }, {
  layout: { hierarchical: { direction: "LR", sortMethod: "directed", levelSeparation: 170, nodeSpacing: 140 } },
  physics: { enabled: false },
  interaction: { hover: true, tooltipDelay: 120 }
});

const legend = document.getElementById("legend");
Object.keys(palette).forEach(type => {
  const item = document.createElement("span");
  item.className = "leg-item";
  const dot = document.createElement("span");
  dot.className = "leg-dot";
  dot.style.background = palette[type].background;
  dot.style.borderColor = palette[type].border;
  item.appendChild(dot);
  item.appendChild(document.createTextNode(type));
  legend.appendChild(item);
});

document.getElementById("summary").textContent = D.analysis.summary || "";
</script>
</body>
</html>
""";

    private const string CoTTemplate = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<title>Chain of Thought — Return Decision</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: system-ui, -apple-system, sans-serif; background: #0f1117; color: #e2e8f0; }
  header { padding: 22px 32px 14px; border-bottom: 1px solid #1e2535; }
  header h1 { font-size: 1.28rem; font-weight: 700; color: #f7fafc; }
  header p { margin-top: 4px; font-size: .84rem; color: #a0aec0; }
  #timeline { padding: 34px 32px; }
  .phase { display: flex; gap: 18px; margin-bottom: 22px; position: relative; }
  .phase::before { content: ""; position: absolute; left: 17px; top: 34px; bottom: -22px; width: 2px; background: #334155; }
  .phase:last-child::before { display: none; }
  .dot { width: 36px; height: 36px; border-radius: 50%; display: grid; place-items: center; font-size: .78rem; font-weight: 700; color: #f8fafc; border: 2px solid #1e2535; flex-shrink: 0; z-index: 1; }
  .dot.facts { background: #3b82f6; }
  .dot.flags { background: #ef4444; }
  .dot.legit { background: #10b981; }
  .dot.policy { background: #f59e0b; }
  .dot.decision { background: #8b5cf6; }
  .card { flex: 1; background: #141820; border: 1px solid #1e2535; border-radius: 10px; padding: 14px 16px; }
  .card h3 { font-size: .85rem; text-transform: uppercase; letter-spacing: .06em; color: #94a3b8; margin-bottom: 6px; }
  .card p { font-size: .9rem; color: #cbd5e1; line-height: 1.55; }
  .score { margin-top: 8px; font-size: .72rem; color: #64748b; }
  #decision { margin: 0 32px 28px; padding: 20px 22px; border-radius: 10px; border: 1px solid #1e2535; background: #141820; }
  #decision h2 { font-size: .8rem; text-transform: uppercase; letter-spacing: .06em; color: #94a3b8; margin-bottom: 10px; }
  #decision pre { white-space: pre-wrap; font-family: inherit; color: #e2e8f0; line-height: 1.6; }
</style>
</head>
<body>
<header>
  <h1>Chain of Thought — Return Decision</h1>
  <p>Facts, red flags, legitimacy, policy, and final decision in an auditable sequence.</p>
</header>

<div id="timeline"></div>

<div id="decision">
  <h2>Final Decision</h2>
  <pre id="decision-text"></pre>
</div>

<script>
const D = __DATA__;
const timeline = document.getElementById("timeline");
const classes = ["facts", "flags", "legit", "policy", "decision"];

D.phases.forEach((p, i) => {
  const phase = document.createElement("div");
  phase.className = "phase";

  const dot = document.createElement("div");
  dot.className = "dot " + (classes[i] || "facts");
  dot.textContent = i + 1;

  const card = document.createElement("div");
  card.className = "card";

  const h = document.createElement("h3");
  h.textContent = p.name;
  card.appendChild(h);

  const summary = document.createElement("p");
  summary.textContent = p.summary;
  card.appendChild(summary);

  if (p.score != null) {
    const score = document.createElement("div");
    score.className = "score";
    score.textContent = "Score: " + p.score + "/10";
    card.appendChild(score);
  }

  phase.appendChild(dot);
  phase.appendChild(card);
  timeline.appendChild(phase);
});

document.getElementById("decision-text").textContent = typeof D.decision === "string" ? D.decision : JSON.stringify(D.decision, null, 2);
</script>
</body>
</html>
""";

    private const string ToolRoutingTemplate = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8" />
<title>Tool Routing with Embeddings</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: system-ui, -apple-system, sans-serif; background: #0f1117; color: #e2e8f0; }
  header { padding: 22px 32px 14px; border-bottom: 1px solid #1e2535; }
  header h1 { font-size: 1.28rem; font-weight: 700; color: #f7fafc; }
  header p { margin-top: 4px; font-size: .84rem; color: #a0aec0; }
  .case { margin: 18px 32px; border: 1px solid #1e2535; border-radius: 10px; background: #141820; overflow: hidden; }
  .case h2 { padding: 14px 18px; font-size: .9rem; color: #f7fafc; border-bottom: 1px solid #1e2535; background: #172033; }
  .case p.prompt { padding: 12px 18px; font-size: .9rem; color: #cbd5e1; border-bottom: 1px solid #1e2535; }
  table { width: 100%; border-collapse: collapse; font-size: .82rem; }
  th, td { padding: 8px 14px; text-align: left; border-bottom: 1px solid #1e2535; }
  th { color: #94a3b8; background: #11141b; }
  td { color: #cbd5e1; }
  td.selected { color: #4ade80; font-weight: 600; }
  .bar-wrap { background: #1e293b; border-radius: 4px; height: 10px; width: 140px; overflow: hidden; }
  .bar { background: #3b82f6; height: 100%; }
  .answer { padding: 12px 18px; font-size: .85rem; color: #e2e8f0; background: #0d1118; border-top: 1px solid #1e2535; }
</style>
</head>
<body>
<header>
  <h1>Tool Routing with Embeddings</h1>
  <p>Each user prompt is embedded, scored against tool exemplars, and routed to a subset of tools.</p>
</header>

<div id="cases"></div>

<script>
const D = __DATA__;
const cases = document.getElementById("cases");

D.forEach(c => {
  const card = document.createElement("div");
  card.className = "case";

  const h = document.createElement("h2");
  h.textContent = c.label;
  card.appendChild(h);

  const prompt = document.createElement("p");
  prompt.className = "prompt";
  prompt.textContent = c.prompt;
  card.appendChild(prompt);

  const table = document.createElement("table");
  const thead = document.createElement("thead");
  thead.innerHTML = "<tr><th>Tool</th><th>Score</th><th>Bar</th><th>Selected</th></tr>";
  table.appendChild(thead);

  const tbody = document.createElement("tbody");
  const sorted = Object.entries(c.scores).sort((a, b) => b[1] - a[1]);
  sorted.forEach(([tool, score]) => {
    const selected = c.selectedKeys.includes(tool);
    const row = document.createElement("tr");
    row.innerHTML = `<td class="${selected ? "selected" : ""}">${tool}</td>` +
                    `<td>${score.toFixed(4)}</td>` +
                    `<td><div class="bar-wrap"><div class="bar" style="width:${Math.max(0, Math.min(100, score * 100))}%"></div></div></td>` +
                    `<td>${selected ? "yes" : ""}</td>`;
    tbody.appendChild(row);
  });
  table.appendChild(tbody);
  card.appendChild(table);

  const answer = document.createElement("div");
  answer.className = "answer";
  answer.textContent = "Answer: " + c.answer;
  card.appendChild(answer);

  cases.appendChild(card);
});
</script>
</body>
</html>
""";
}
