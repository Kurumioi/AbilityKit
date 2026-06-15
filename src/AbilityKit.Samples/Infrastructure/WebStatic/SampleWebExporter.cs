using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AbilityKit.Samples.Abstractions;
using AbilityKit.Samples.Logic;

namespace AbilityKit.Samples.Infrastructure.WebStatic
{
    internal static class SampleWebExporter
    {
        public static string Export(string outputDirectory, SampleRunOptions options)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = "sample-web";

            Directory.CreateDirectory(outputDirectory);

            var catalog = SampleCatalogProvider.CreateCatalog();
            var executor = new SampleExecutionService(catalog, SampleEnvironmentFactory.Create);
            var exported = new List<WebSampleEntry>();

            var exportEntries = catalog.Entries
                .Where(entry => entry.Tags.Any(tag =>
                    string.Equals(tag, "web", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tag, "web-export", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var entry in exportEntries)
            {
                var logger = new BufferedSampleLogger();
                var runOptions = new SampleRunOptions
                {
                    ExecutionMode = options.ExecutionMode,
                    HostKind = SampleHostKind.Web,
                    HostCapabilities = SampleHostCapabilities.ForHost(SampleHostKind.Web),
                    WriteConsole = false,
                    WriteFile = false,
                    OutputDirectory = outputDirectory
                };
                SeedDefaultInputs(entry, runOptions);

                var result = executor.Run(entry, logger, runOptions);
                exported.Add(WebSampleEntry.From(entry, logger.Entries, result));
            }

            var page = RenderPage(new WebSampleDocument
            {
                GeneratedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
                OutputSchemaVersion = SampleOutputContract.SchemaVersion,
                Samples = exported
            });

            var path = Path.GetFullPath(Path.Combine(outputDirectory, "index.html"));
            File.WriteAllText(path, page, Encoding.UTF8);
            return path;
        }

        private static void SeedDefaultInputs(SampleCatalogEntry entry, SampleRunOptions runOptions)
        {
            foreach (var field in entry.InputFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Key) && !runOptions.Inputs.ContainsKey(field.Key))
                    runOptions.Inputs[field.Key] = field.DefaultValue;
            }
        }

        private static string RenderPage(WebSampleDocument document)
        {
            var json = JsonSerializer.Serialize(document, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            return @"<!doctype html>
<html lang=""zh-CN"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>AbilityKit Samples</title>
  <style>
    :root { color-scheme: light; --bg:#f6f7f9; --panel:#fff; --line:#d8dee8; --text:#1f2937; --muted:#64748b; --accent:#0f766e; --accent-soft:#ccfbf1; --bad:#b91c1c; --warn:#a16207; --info:#2563eb; }
    * { box-sizing: border-box; }
    body { margin:0; font-family: Segoe UI, Microsoft YaHei, Arial, sans-serif; background:var(--bg); color:var(--text); }
    .app { display:grid; grid-template-columns: 360px 1fr; min-height:100vh; }
    aside { background:var(--panel); border-right:1px solid var(--line); padding:16px; overflow:auto; }
    main { padding:20px; overflow:auto; }
    h1 { font-size:20px; margin:0 0 4px; }
    h2 { font-size:18px; margin:0 0 8px; }
    .meta { color:var(--muted); font-size:13px; margin-bottom:14px; }
    .search { width:100%; padding:10px 12px; border:1px solid var(--line); border-radius:6px; margin:10px 0 10px; font-size:14px; }
    .capability-map { margin:0 0 14px; padding:10px; border:1px solid var(--line); border-radius:8px; background:#f8fafc; }
    .capability-map-title { color:var(--muted); font-size:12px; font-weight:700; text-transform:uppercase; margin-bottom:8px; }
    .capability-map-grid { display:flex; flex-wrap:wrap; gap:6px; }
    .capability-chip { border:1px solid var(--line); border-radius:999px; background:#fff; color:var(--text); padding:5px 8px; cursor:pointer; font-size:12px; }
    .capability-chip:hover, .capability-chip.active { border-color:var(--accent); background:#ecfdf5; color:var(--accent); }
    .capability-count { display:inline-block; margin-left:6px; color:var(--muted); }
    .category { margin:16px 0 8px; color:var(--muted); font-weight:600; font-size:13px; text-transform:uppercase; }
    button.sample { width:100%; display:block; text-align:left; background:#fff; border:1px solid var(--line); border-radius:6px; padding:10px; margin:6px 0; cursor:pointer; }
    button.sample:hover, button.sample.active { border-color:var(--accent); background:#ecfdf5; }
    .title { font-weight:650; font-size:14px; }
    .id { color:var(--muted); font-size:12px; margin-top:3px; overflow-wrap:anywhere; }
    .toolbar { display:flex; gap:8px; align-items:center; justify-content:space-between; margin-bottom:12px; }
    .badge { display:inline-block; border:1px solid var(--line); border-radius:999px; padding:3px 8px; font-size:12px; color:var(--muted); margin-right:6px; margin-bottom:4px; }
    .ok { color:var(--accent); }
    .fail { color:var(--bad); }
    .viewer { display:grid; grid-template-columns:minmax(360px, 1.1fr) minmax(320px, .9fr); gap:14px; align-items:start; }
    .panel { background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:14px; }
    .stage { width:100%; height:360px; display:block; background:linear-gradient(180deg, #f8fafc 0%, #eef2f7 100%); border:1px solid var(--line); border-radius:8px; }
    .controls { display:flex; flex-wrap:wrap; gap:8px; align-items:center; margin-top:12px; }
    .controls button, .controls select { border:1px solid var(--line); background:#fff; color:var(--text); border-radius:6px; padding:7px 10px; cursor:pointer; }
    .controls button:hover { border-color:var(--accent); color:var(--accent); }
    .timeline { width:100%; margin-top:12px; accent-color:var(--accent); }
    .event-card { margin-top:10px; padding:10px; border-radius:8px; background:#f8fafc; border:1px solid var(--line); min-height:64px; }
    .event-kind { font-size:12px; color:var(--muted); text-transform:uppercase; margin-bottom:4px; }
    .event-text { font-size:14px; white-space:pre-wrap; overflow-wrap:anywhere; }
    .guide { margin-top:12px; display:grid; gap:10px; }
    .guide-grid { display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:8px; }
    .guide-card { padding:10px; border:1px solid var(--line); border-radius:8px; background:#fff; }
    .guide-title { font-size:12px; color:var(--muted); font-weight:700; text-transform:uppercase; margin-bottom:5px; }
    .guide-text { font-size:13px; line-height:1.5; overflow-wrap:anywhere; }
    .visual-flow { display:flex; flex-wrap:wrap; gap:8px; align-items:center; padding:10px; border:1px dashed #99f6e4; border-radius:8px; background:#f0fdfa; }
    .visual-step { max-width:180px; padding:8px 10px; border-radius:999px; background:#fff; border:1px solid #5eead4; color:#115e59; font-size:12px; }
    .visual-arrow { color:var(--accent); font-weight:700; }
    .learning { margin-top:12px; display:grid; gap:8px; }
    .learning-grid { display:grid; grid-template-columns:repeat(2, minmax(0, 1fr)); gap:8px; }
    .learning-list { padding-left:18px; margin:6px 0 0; color:var(--text); font-size:13px; line-height:1.5; }
    .learning-list li { margin:2px 0; }
    .pathway { margin-top:12px; display:grid; gap:8px; }
    .pathway-row { display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:8px; }
    .pathway-card { padding:10px; border:1px solid var(--line); border-radius:8px; background:#fff; }
    .pathway-card.primary { border-color:#5eead4; background:#f0fdfa; }
    .pathway-button { width:100%; margin-top:8px; border:1px solid var(--line); background:#fff; border-radius:6px; padding:7px 8px; cursor:pointer; text-align:left; color:var(--accent); }
    .pathway-button:hover { border-color:var(--accent); background:#ecfdf5; }
    .inputs { margin-top:12px; display:grid; gap:8px; }
    .input-grid { display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:8px; }
    .input-card { padding:10px; border:1px solid var(--line); border-radius:8px; background:#fff; }
    .input-label { display:block; font-size:12px; color:var(--muted); font-weight:700; text-transform:uppercase; margin-bottom:6px; }
    .input-control { width:100%; border:1px solid var(--line); border-radius:6px; padding:7px 8px; font-size:13px; }
    .input-help { margin-top:6px; color:var(--muted); font-size:12px; line-height:1.4; }
    .walkthrough { margin-top:12px; display:grid; gap:8px; }
    .walkthrough-step { border:1px solid var(--line); border-radius:8px; padding:10px; background:#fff; cursor:pointer; }
    .walkthrough-step.active { border-color:var(--accent); background:#ecfdf5; }
    .walkthrough-title { font-weight:700; font-size:13px; margin-bottom:4px; }
    .walkthrough-source { color:var(--info); font-family:Consolas, monospace; font-size:12px; overflow-wrap:anywhere; margin-bottom:6px; }
    .walkthrough-text { font-size:13px; line-height:1.5; overflow-wrap:anywhere; }
    .walkthrough-code { margin-top:8px; padding:10px; border-radius:8px; background:#0f172a; color:#e2e8f0; overflow:auto; font-family:Consolas, monospace; font-size:12px; line-height:1.45; white-space:pre; }
    .walkthrough-hint { margin-top:6px; color:var(--muted); font-size:12px; }
    .code-badge { color:#115e59; border-color:#5eead4; background:#f0fdfa; }
    .log { max-height:520px; overflow:auto; }
    .row { padding:4px 6px; white-space:pre-wrap; overflow-wrap:anywhere; font-family: Consolas, Microsoft YaHei, monospace; font-size:13px; border-radius:5px; }
    .row.active { background:var(--accent-soft); outline:1px solid #5eead4; }
    .section { font-weight:700; margin-top:8px; }
    .warn { color:var(--warn); }
    .error { color:var(--bad); }
    .kv .key { color:var(--muted); }
    @media (max-width: 1000px) { .viewer { grid-template-columns:1fr; } .guide-grid { grid-template-columns:1fr; } }
    @media (max-width: 800px) { .app { grid-template-columns:1fr; } aside { border-right:0; border-bottom:1px solid var(--line); max-height:45vh; } }
  </style>
</head>
<body>
<div class=""app"">
  <aside>
    <h1>AbilityKit Samples</h1>
    <div class=""meta"" id=""generated""></div>
    <input class=""search"" id=""search"" placeholder=""Search title, id, category, tags, modules..."">
    <div class=""capability-map"" id=""capabilityMap""></div>
    <div id=""list""></div>
  </aside>
  <main>
    <div class=""toolbar"">
      <div>
        <h2 id=""sampleTitle"">Select a sample</h2>
        <div class=""meta"" id=""sampleMeta""></div>
      </div>
      <div id=""sampleState""></div>
    </div>
    <div class=""viewer"">
      <section class=""panel"">
        <canvas class=""stage"" id=""stage"" width=""960"" height=""540""></canvas>
        <input class=""timeline"" id=""timeline"" type=""range"" min=""0"" max=""0"" value=""0"">
        <div class=""controls"">
          <button id=""play"">播放</button>
          <button id=""prev"">上一帧</button>
          <button id=""next"">下一帧</button>
          <select id=""speed"">
            <option value=""0.5"">0.5x</option>
            <option value=""1"" selected>1x</option>
            <option value=""2"">2x</option>
            <option value=""4"">4x</option>
          </select>
          <span class=""meta"" id=""frameInfo""></span>
        </div>
        <div class=""event-card"">
          <div class=""event-kind"" id=""eventKind""></div>
          <div class=""event-text"" id=""eventText""></div>
        </div>
        <div class=""pathway"" id=""pathway""></div>
        <div class=""guide"" id=""guide""></div>
        <div class=""learning"" id=""learning""></div>
        <div class=""inputs"" id=""inputs""></div>
        <div class=""walkthrough"" id=""walkthrough""></div>
      </section>
      <section class=""panel log"" id=""log""></section>
    </div>
  </main>
</div>
<script>
const DATA = " + json + @";
const list = document.getElementById('list');
const log = document.getElementById('log');
const search = document.getElementById('search');
const capabilityMap = document.getElementById('capabilityMap');
const generated = document.getElementById('generated');
const sampleTitle = document.getElementById('sampleTitle');
const sampleMeta = document.getElementById('sampleMeta');
const sampleState = document.getElementById('sampleState');
const stage = document.getElementById('stage');
const ctx = stage.getContext('2d');
const timeline = document.getElementById('timeline');
const playButton = document.getElementById('play');
const prevButton = document.getElementById('prev');
const nextButton = document.getElementById('next');
const speed = document.getElementById('speed');
const frameInfo = document.getElementById('frameInfo');
const eventKind = document.getElementById('eventKind');
const eventText = document.getElementById('eventText');
const pathway = document.getElementById('pathway');
const guide = document.getElementById('guide');
const learning = document.getElementById('learning');
const inputs = document.getElementById('inputs');
const walkthrough = document.getElementById('walkthrough');
generated.textContent = '导出时间：' + DATA.generatedAt + '。重新导出后刷新本文件即可。';
let activeId = DATA.samples.find(s => s.codeWalkthrough && s.codeWalkthrough.length > 0)?.id || DATA.samples[0]?.id || '';
let activeFrame = 0;
let playing = false;
let lastTick = 0;

function renderCapabilityMap() {
  const q = search.value.trim().toLowerCase();
  const counts = new Map();
  for (const sample of DATA.samples) {
    for (const module of sample.modules || []) {
      counts.set(module, (counts.get(module) || 0) + 1);
    }
  }

  const chips = Array.from(counts.entries()).sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]));
  capabilityMap.innerHTML = '<div class=""capability-map-title"">框架能力地图 · 按模块筛选示例</div><div class=""capability-map-grid"">'
    + chips.map(([module, count]) => '<button class=""capability-chip' + (q === module.toLowerCase() ? ' active' : '') + '"" data-module-filter=""' + escapeHtml(module) + '"">' + escapeHtml(module) + '<span class=""capability-count"">' + count + '</span></button>').join('')
    + '</div>';
}

function renderList() {
  const q = search.value.trim().toLowerCase();
  const items = DATA.samples.filter(s => !q || (s.title + ' ' + s.id + ' ' + s.category + ' ' + s.tags.join(' ') + ' ' + s.modules.join(' ')).toLowerCase().includes(q));
  const groups = new Map();
  for (const item of items) {
    if (!groups.has(item.category)) groups.set(item.category, []);
    groups.get(item.category).push(item);
  }
  list.innerHTML = '';
  for (const [category, samples] of groups) {
    const label = document.createElement('div');
    label.className = 'category';
    label.textContent = category;
    list.appendChild(label);
    for (const sample of samples) {
      const button = document.createElement('button');
      button.className = 'sample' + (sample.id === activeId ? ' active' : '');
      const hasCode = sample.codeWalkthrough && sample.codeWalkthrough.length > 0;
      button.innerHTML = '<div class=""title"">' + escapeHtml(sample.title) + (hasCode ? ' <span class=""badge code-badge"">代码讲解</span>' : '') + '</div><div class=""id"">' + escapeHtml(sample.id) + '</div>';
      button.onclick = () => { activeId = sample.id; activeFrame = 0; playing = false; renderCapabilityMap(); renderList(); renderSample(); };
      list.appendChild(button);
    }
  }
}

function renderSample() {
  const sample = currentSample();
  if (!sample) return;
  sampleTitle.textContent = sample.title;
  sampleMeta.innerHTML = buildBadges([sample.id, sample.category, sample.status, sample.level, ...sample.modules, sample.codeWalkthrough && sample.codeWalkthrough.length ? '代码讲解' : '']);
  sampleState.innerHTML = sample.succeeded ? '<span class=""ok"">Succeeded</span>' : '<span class=""fail"">Failed</span>';
  activeFrame = clamp(activeFrame, 0, Math.max(0, playbackFrames(sample).length - 1));
  timeline.max = Math.max(0, playbackFrames(sample).length - 1);
  timeline.value = activeFrame;
  playButton.textContent = playing ? '暂停' : '播放';
  renderCanvas(sample);
  renderEvent(sample);
  renderPathway(sample);
  renderGuide(sample);
  renderLearning(sample);
  renderInputs(sample);
  renderWalkthrough(sample);
  renderLog(sample);
}

function renderCanvas(sample) {
  const content = sample.guide || {};
  const steps = content.visualSteps && content.visualSteps.length ? content.visualSteps : [sample.title, sample.category, sample.status];
  const template = String(sample.visualTemplate || content.visualKind || 'flow').toLowerCase();
  ctx.clearRect(0, 0, stage.width, stage.height);
  drawSemanticBackground(sample, content, template);
  if (template === 'pipeline-flow') {
    drawPipelineFlowTemplate(sample, steps);
  } else if (template === 'stack') {
    drawStackDiagram(steps);
  } else if (template === 'timeline') {
    drawTimelineDiagram(steps);
  } else {
    drawFlowDiagram(steps);
  }
  drawCanvasFooter(sample);
}

function drawSemanticBackground(sample, content, template) {
  const gradient = ctx.createLinearGradient(0, 0, stage.width, stage.height);
  gradient.addColorStop(0, '#f8fafc');
  gradient.addColorStop(1, '#ecfdf5');
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, stage.width, stage.height);
  ctx.strokeStyle = '#d8dee8';
  ctx.strokeRect(20, 20, stage.width - 40, stage.height - 40);
  ctx.fillStyle = '#0f172a';
  ctx.font = '22px Segoe UI, Microsoft YaHei, sans-serif';
  ctx.fillText(sample.title, 34, 54);
  ctx.fillStyle = '#64748b';
  ctx.font = '14px Segoe UI, Microsoft YaHei, sans-serif';
  ctx.fillText('语义图模板：' + (template || content.visualKind || 'flow') + ' · ' + sample.id, 34, 78);
}

function drawPipelineFlowTemplate(sample, steps) {
  const model = sample.visualModel || {};
  const nodes = model.nodes && model.nodes.length
    ? model.nodes
    : (steps && steps.length ? steps.map(step => ({ id: step, label: step, kind: 'phase', description: '' })) : []);
  const labels = nodes.map(node => node.label || node.id);
  const active = activeSemanticIndex(labels);
  const y = stage.height / 2 - 58;
  const boxWidth = Math.max(128, Math.min(168, (stage.width - 180) / Math.max(1, nodes.length)));
  const gap = nodes.length <= 1 ? 0 : (stage.width - 100 - boxWidth * nodes.length) / (nodes.length - 1);
  for (let i = 0; i < nodes.length; i++) {
    const node = nodes[i];
    const x = 50 + i * (boxWidth + gap);
    const isActive = i === active;
    drawRoundRect(x, y, boxWidth, 100, 14, isActive ? '#ccfbf1' : '#ffffff', isActive ? '#0f766e' : '#5eead4');
    ctx.fillStyle = isActive ? '#0f766e' : '#64748b';
    ctx.font = '12px Consolas, Microsoft YaHei, monospace';
    ctx.fillText(node.kind || 'phase', x + 14, y + 24);
    drawCenteredText(node.label || node.id, x + boxWidth / 2, y + 52, boxWidth - 24, '#115e59');
    if (node.description) drawCenteredText(node.description, x + boxWidth / 2, y + 78, boxWidth - 22, '#64748b');
    if (i < nodes.length - 1) drawTemplateEdge(model, nodes, i, x, y, boxWidth, gap);
  }

  const frame = currentVisualFrame(sample);
  const metrics = model.metrics && model.metrics.length
    ? model.metrics.map(metric => (metric.label || metric.key) + ': ' + metric.value)
    : (frame && frame.stateChanges && frame.stateChanges.length ? frame.stateChanges : ['输入参数', '阶段执行', '结构化输出']);
  for (let i = 0; i < Math.min(3, metrics.length); i++) {
    const x = 80 + i * 260;
    drawRoundRect(x, y + 142, 220, 48, 10, '#f8fafc', '#d8dee8');
    drawCanvasText(metrics[i], x + 14, y + 40 + 142, 192, '#1f2937');
  }
}

function drawTemplateEdge(model, nodes, index, x, y, boxWidth, gap) {
  const from = nodes[index];
  const to = nodes[index + 1];
  const edge = model.edges && model.edges.find(item => item.from === from.id && item.to === to.id);
  drawArrow(x + boxWidth + 8, y + 50, x + boxWidth + gap - 10, y + 50);
  if (edge && edge.label) {
    ctx.fillStyle = '#64748b';
    ctx.font = '11px Consolas, Microsoft YaHei, monospace';
    ctx.fillText(edge.label, x + boxWidth + 18, y + 36);
  }
}

function drawFlowDiagram(steps) {
  const y = stage.height / 2 - 34;
  const boxWidth = Math.max(120, Math.min(170, (stage.width - 150) / Math.max(1, steps.length)));
  const gap = steps.length <= 1 ? 0 : (stage.width - 80 - boxWidth * steps.length) / (steps.length - 1);
  for (let i = 0; i < steps.length; i++) {
    const x = 40 + i * (boxWidth + gap);
    drawRoundRect(x, y, boxWidth, 82, 12, i === activeSemanticIndex(steps) ? '#ccfbf1' : '#ffffff', '#5eead4');
    drawCenteredText(steps[i], x + boxWidth / 2, y + 38, boxWidth - 20, '#115e59');
    if (i < steps.length - 1) drawArrow(x + boxWidth + 8, y + 41, x + boxWidth + gap - 10, y + 41);
  }
}

function drawStackDiagram(steps) {
  const boxWidth = 520;
  const boxHeight = 58;
  const x = (stage.width - boxWidth) / 2;
  const startY = 120;
  for (let i = 0; i < steps.length; i++) {
    const y = startY + i * (boxHeight + 12);
    drawRoundRect(x, y, boxWidth, boxHeight, 10, i === activeSemanticIndex(steps) ? '#ccfbf1' : '#ffffff', '#5eead4');
    ctx.fillStyle = '#0f766e';
    ctx.font = '13px Consolas, Microsoft YaHei, monospace';
    ctx.fillText('Layer ' + (i + 1), x + 18, y + 35);
    drawCanvasText(steps[i], x + 110, y + 35, boxWidth - 130, '#1f2937');
  }
}

function drawTimelineDiagram(steps) {
  const startX = 90;
  const endX = stage.width - 90;
  const y = stage.height / 2;
  ctx.strokeStyle = '#0f766e';
  ctx.lineWidth = 4;
  ctx.beginPath(); ctx.moveTo(startX, y); ctx.lineTo(endX, y); ctx.stroke();
  for (let i = 0; i < steps.length; i++) {
    const x = startX + (steps.length <= 1 ? 0 : (endX - startX) * i / (steps.length - 1));
    const active = i === activeSemanticIndex(steps);
    ctx.fillStyle = active ? '#0f766e' : '#ffffff';
    ctx.strokeStyle = '#0f766e';
    ctx.lineWidth = 3;
    ctx.beginPath(); ctx.arc(x, y, active ? 18 : 13, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
    drawCenteredText(steps[i], x, y + 52, 150, '#1f2937');
  }
}

function activeSemanticIndex(steps) {
  if (!steps.length) return -1;
  const frame = currentVisualFrame(currentSample());
  if (frame && frame.visualStep) {
    const index = steps.findIndex(step => String(step).toLowerCase() === String(frame.visualStep).toLowerCase());
    if (index >= 0) return index;
  }

  const total = Math.max(1, playbackFrames(currentSample()).length || steps.length);
  return clamp(Math.floor(activeFrame / total * steps.length), 0, steps.length - 1);
}

function drawCanvasFooter(sample) {
  const frame = currentVisualFrame(sample);
  const event = sample.timeline[activeFrame];
  ctx.fillStyle = 'rgba(255,255,255,.82)';
  ctx.fillRect(28, stage.height - 76, stage.width - 56, 48);
  ctx.strokeStyle = '#d8dee8';
  ctx.strokeRect(28, stage.height - 76, stage.width - 56, 48);
  ctx.fillStyle = '#64748b';
  ctx.font = '14px Consolas, Microsoft YaHei, monospace';
  ctx.fillText((frame ? 'visual frame ' : 'log frame ') + (activeFrame + 1) + ' / ' + Math.max(1, playbackFrames(sample).length), 44, stage.height - 48);
  if (frame) {
    drawCanvasText(frame.title + ' · ' + frame.description, 210, stage.height - 48, stage.width - 260, '#1f2937');
  } else if (event) {
    drawCanvasText(event.label, 190, stage.height - 48, stage.width - 240, '#1f2937');
  }
}

function drawRoundRect(x, y, width, height, radius, fill, stroke) {
  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.lineTo(x + width - radius, y);
  ctx.quadraticCurveTo(x + width, y, x + width, y + radius);
  ctx.lineTo(x + width, y + height - radius);
  ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height);
  ctx.lineTo(x + radius, y + height);
  ctx.quadraticCurveTo(x, y + height, x, y + height - radius);
  ctx.lineTo(x, y + radius);
  ctx.quadraticCurveTo(x, y, x + radius, y);
  ctx.closePath();
  ctx.fillStyle = fill;
  ctx.fill();
  ctx.strokeStyle = stroke;
  ctx.lineWidth = 2;
  ctx.stroke();
}

function drawArrow(fromX, fromY, toX, toY) {
  ctx.strokeStyle = '#0f766e';
  ctx.fillStyle = '#0f766e';
  ctx.lineWidth = 3;
  ctx.beginPath(); ctx.moveTo(fromX, fromY); ctx.lineTo(toX, toY); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(toX, toY); ctx.lineTo(toX - 10, toY - 6); ctx.lineTo(toX - 10, toY + 6); ctx.closePath(); ctx.fill();
}

function drawCenteredText(text, centerX, y, maxWidth, color) {
  ctx.fillStyle = color;
  ctx.font = '14px Segoe UI, Microsoft YaHei, sans-serif';
  const lines = splitCanvasLines(text, maxWidth, 2);
  for (let i = 0; i < lines.length; i++) {
    ctx.fillText(lines[i], centerX - ctx.measureText(lines[i]).width / 2, y + i * 18);
  }
}

function drawCanvasText(text, x, y, maxWidth, color) {
  ctx.fillStyle = color;
  ctx.font = '14px Segoe UI, Microsoft YaHei, sans-serif';
  const lines = splitCanvasLines(text, maxWidth, 1);
  if (lines.length) ctx.fillText(lines[0], x, y);
}

function splitCanvasLines(text, maxWidth, maxLines) {
  const words = String(text || '').split(/\s+/);
  const lines = [];
  let line = '';
  for (const word of words) {
    const test = line ? line + ' ' + word : word;
    if (ctx.measureText(test).width > maxWidth && line) {
      lines.push(line);
      line = word;
      if (lines.length >= maxLines) return lines;
    } else {
      line = test;
    }
  }
  if (line && lines.length < maxLines) lines.push(line);
  return lines;
}

function renderEvent(sample) {
  const frames = playbackFrames(sample);
  const frame = currentVisualFrame(sample);
  const event = sample.timeline[activeFrame];
  frameInfo.textContent = frames.length ? 'Frame ' + (activeFrame + 1) + ' / ' + frames.length : 'No timeline';

  if (frame) {
    const details = [];
    if (frame.description) details.push(frame.description);
    if (frame.stateChanges && frame.stateChanges.length) details.push('状态变化：' + frame.stateChanges.join('；'));
    if (frame.highlights && frame.highlights.length) details.push('高亮：' + frame.highlights.join('、'));
    eventKind.textContent = 'VisualFrame · ' + frame.visualStep;
    eventText.textContent = frame.title + '\n' + details.join('\n');
    return;
  }

  eventKind.textContent = event ? event.kind + ' · t=' + event.time.toFixed(1) + 's' : 'No event';
  eventText.textContent = event ? event.label : '这个样例没有产生日志事件。';
}

function renderPathway(sample) {
  const currentIndex = DATA.samples.findIndex(item => item.id === sample.id);
  const previous = currentIndex > 0 ? DATA.samples[currentIndex - 1] : null;
  const nextEntries = (sample.next || []).map(id => DATA.samples.find(item => item.id === id)).filter(Boolean);
  const fallbackNext = currentIndex >= 0 && currentIndex < DATA.samples.length - 1 ? DATA.samples[currentIndex + 1] : null;
  const recommended = nextEntries.length ? nextEntries : (fallbackNext ? [fallbackNext] : []);

  const cards = [];
  cards.push(renderPathwayCard('当前位置', sample, '第 ' + (currentIndex + 1) + ' / ' + DATA.samples.length + ' 个正式示例', true));
  if (previous) cards.push(renderPathwayCard('上一步', previous, '用于回顾前置概念', false));
  if (recommended.length) cards.push(renderPathwayCard('推荐下一步', recommended[0], sample.next && sample.next.length ? '来自 manifest.next' : '按导出顺序推荐', false));

  pathway.innerHTML = '<div class=""guide-title"">学习路径 · 当前示例在整体路线中的位置</div><div class=""pathway-row"">' + cards.join('') + '</div>';
}

function renderPathwayCard(label, sample, hint, primary) {
  const disabled = primary ? ' disabled' : '';
  const button = primary ? '' : '<button class=""pathway-button"" data-sample-id=""' + escapeHtml(sample.id) + '"">打开这个示例</button>';
  return '<div class=""pathway-card' + (primary ? ' primary' : '') + '"">'
    + '<div class=""guide-title"">' + escapeHtml(label) + '</div>'
    + '<div class=""walkthrough-title"">' + escapeHtml(sample.title) + '</div>'
    + '<div class=""input-help"">' + escapeHtml(sample.id) + '</div>'
    + '<div class=""guide-text"">' + escapeHtml(hint || '') + '</div>'
    + button
    + '</div>';
}

function renderGuide(sample) {
  const content = sample.guide || {};
  const cards = [
    ['用途', content.purpose],
    ['观察点', content.observe],
    ['迁移到项目', content.takeaway]
  ].filter(x => x[1]);
  const steps = content.visualSteps || [];
  if (!cards.length && steps.length === 0) {
    guide.innerHTML = '<div class=""event-card""><div class=""event-kind"">Guide</div><div class=""event-text"">这个示例暂未配置图文讲解，仍可通过时间线和日志理解运行过程。</div></div>';
    return;
  }

  const cardHtml = cards.map(x => '<div class=""guide-card""><div class=""guide-title"">' + escapeHtml(x[0]) + '</div><div class=""guide-text"">' + escapeHtml(x[1]) + '</div></div>').join('');
  const visualHtml = steps.length
    ? '<div class=""visual-flow"">' + steps.map((step, index) => '<span class=""visual-step"">' + escapeHtml(step) + '</span>' + (index < steps.length - 1 ? '<span class=""visual-arrow"">→</span>' : '')).join('') + '</div>'
    : '';
  guide.innerHTML = '<div class=""guide-grid"">' + cardHtml + '</div>' + visualHtml;
}

function renderLearning(sample) {
  const contract = sample.learningContract || {};
  const blocks = [
    ['学习目标', contract.summary],
    ['执行模式', contract.executionHint],
    ['能力点', contract.capabilities],
    ['API 导览', contract.apiHighlights],
    ['核心概念', contract.concepts],
    ['输入提示', contract.inputHints],
    ['输出提示', contract.outputHints]
  ];

  const html = blocks
    .filter(item => Array.isArray(item[1]) ? item[1].length : String(item[1] || '').trim().length > 0)
    .map(item => {
      const value = Array.isArray(item[1])
        ? '<ul class=""learning-list"">' + item[1].map(x => '<li>' + escapeHtml(x) + '</li>').join('') + '</ul>'
        : '<div class=""guide-text"">' + escapeHtml(item[1]) + '</div>';
      return '<div class=""guide-card""><div class=""guide-title"">' + escapeHtml(item[0]) + '</div>' + value + '</div>';
    })
    .join('');

  const checkpoints = sample.learningCheckpoints || [];
  const checkpointHtml = checkpoints.length
    ? '<div class=""guide-title"">学习检查点 · 看完后应该能回答</div><div class=""learning-grid"">'
      + checkpoints.map(renderCheckpoint).join('')
      + '</div>'
    : '';

  learning.innerHTML = (html
    ? '<div class=""guide-title"">学习契约 · 快速理解框架能力与使用方式</div><div class=""learning-grid"">' + html + '</div>'
    : '') + checkpointHtml;
}

function renderCheckpoint(checkpoint) {
  const apiHtml = checkpoint.relatedApis && checkpoint.relatedApis.length
    ? '<ul class=""learning-list"">' + checkpoint.relatedApis.map(api => '<li>' + escapeHtml(api) + '</li>').join('') + '</ul>'
    : '';
  const hint = [checkpoint.relatedVisualStep, checkpoint.relatedOutputHint].filter(Boolean).join(' · ');
  return '<div class=""guide-card"">'
    + '<div class=""guide-title"">' + escapeHtml(checkpoint.title || checkpoint.id || 'Checkpoint') + '</div>'
    + (checkpoint.goal ? '<div class=""guide-text"">目标：' + escapeHtml(checkpoint.goal) + '</div>' : '')
    + (checkpoint.question ? '<div class=""guide-text"">问题：' + escapeHtml(checkpoint.question) + '</div>' : '')
    + (checkpoint.expectedAnswer ? '<div class=""guide-text"">判断标准：' + escapeHtml(checkpoint.expectedAnswer) + '</div>' : '')
    + (hint ? '<div class=""input-help"">关联：' + escapeHtml(hint) + '</div>' : '')
    + apiHtml
    + '</div>';
}

function renderInputs(sample) {
  const fields = sample.inputFields || [];
  if (!fields.length) {
    inputs.innerHTML = '';
    return;
  }

  const html = fields.map(field => {
    const type = String(field.type || 'text').toLowerCase();
    const required = field.required ? ' required' : '';
    const value = escapeHtml(field.defaultValue || '');
    let control = '';
    if (type === 'select') {
      control = '<select class=""input-control"" data-input-key=""' + escapeHtml(field.key) + '""' + required + '>'
        + (field.options || []).map(option => '<option value=""' + escapeHtml(option) + '""' + (option === field.defaultValue ? ' selected' : '') + '>' + escapeHtml(option) + '</option>').join('')
        + '</select>';
    } else if (type === 'boolean') {
      control = '<select class=""input-control"" data-input-key=""' + escapeHtml(field.key) + '""' + required + '>'
        + '<option value=""true""' + (String(field.defaultValue).toLowerCase() === 'true' ? ' selected' : '') + '>true</option>'
        + '<option value=""false""' + (String(field.defaultValue).toLowerCase() === 'false' ? ' selected' : '') + '>false</option>'
        + '</select>';
    } else {
      const htmlType = type === 'number' ? 'number' : 'text';
      control = '<input class=""input-control"" data-input-key=""' + escapeHtml(field.key) + '"" type=""' + htmlType + '"" value=""' + value + '""'
        + (field.min ? ' min=""' + escapeHtml(field.min) + '""' : '')
        + (field.max ? ' max=""' + escapeHtml(field.max) + '""' : '')
        + (field.step ? ' step=""' + escapeHtml(field.step) + '""' : '')
        + required + '>';
    }

    return '<div class=""input-card"">'
      + '<label class=""input-label"">' + escapeHtml(field.label || field.key) + '</label>'
      + control
      + (field.helpText ? '<div class=""input-help"">' + escapeHtml(field.helpText) + '</div>' : '')
      + '</div>';
  }).join('');

  inputs.innerHTML = '<div class=""guide-title"">输入契约 · 宿主可渲染控件与后续执行参数</div><div class=""input-grid"">' + html + '</div>';
}

function renderWalkthrough(sample) {
  const steps = sample.codeWalkthrough || [];
  if (!steps.length) {
    walkthrough.innerHTML = '<div class=""event-card""><div class=""event-kind"">Code Walkthrough</div><div class=""event-text"">这个示例暂未配置源码讲解。后续可以在 manifest 中补充 sourceFile、line range 和 outputHint。</div></div>';
    return;
  }

  const active = activeWalkthroughIndex(sample, steps);
  walkthrough.innerHTML = '<div class=""guide-title"">代码讲解 · 展示源码片段、代码作用和对应输出</div>' + steps.map((step, index) => {
    const source = step.sourceFile + ':' + step.startLine + '-' + step.endLine;
    const hint = [step.visualStep, step.outputHint].filter(Boolean).join(' · ');
    return '<div class=""walkthrough-step' + (index === active ? ' active' : '') + '"" data-index=""' + index + '"">'
      + '<div class=""walkthrough-title"">' + escapeHtml(step.title) + '</div>'
      + '<div class=""walkthrough-source"">' + escapeHtml(source) + '</div>'
      + '<div class=""walkthrough-text"">作用：' + escapeHtml(step.explanation) + '</div>'
      + '<pre class=""walkthrough-code"">' + escapeHtml(step.code || '源码片段未导出，请检查 sourceFile 和行号范围。') + '</pre>'
      + (hint ? '<div class=""walkthrough-hint"">关联输出/图示：' + escapeHtml(hint) + '</div>' : '')
      + '</div>';
  }).join('');

  for (const node of walkthrough.querySelectorAll('.walkthrough-step')) {
    node.onclick = () => {
      const step = steps[Number(node.dataset.index)];
      activeFrame = frameByOutputHint(sample, step.outputHint);
      renderSample();
    };
  }
}

function activeWalkthroughIndex(sample, steps) {
  const event = sample.timeline[activeFrame];
  if (!event) return 0;
  const label = String(event.label || '').toLowerCase();
  const index = steps.findIndex(step => {
    const hint = String(step.outputHint || '').toLowerCase();
    const visual = String(step.visualStep || '').toLowerCase();
    return hint && label.includes(hint) || visual && label.includes(visual);
  });
  return index < 0 ? 0 : index;
}

function frameByOutputHint(sample, outputHint) {
  const hint = String(outputHint || '').toLowerCase();
  if (!hint) return activeFrame;
  const frame = sample.timeline.findIndex(x => String(x.label || '').toLowerCase().includes(hint));
  return frame < 0 ? activeFrame : frame;
}

function renderLog(sample) {
  log.innerHTML = '';
  for (let i = 0; i < sample.logs.length; i++) {
    const entry = sample.logs[i];
    const row = document.createElement('div');
    row.className = 'row ' + kindClass(entry.kind) + (i === activeLogIndex(sample) ? ' active' : '');
    row.innerHTML = formatEntry(entry);
    row.onclick = () => { activeFrame = frameByLogIndex(sample, i); renderSample(); };
    log.appendChild(row);
  }
  const active = log.querySelector('.row.active');
  if (active) active.scrollIntoView({ block: 'nearest' });
}

function step(delta) {
  const sample = currentSample();
  if (!sample) return;
  activeFrame = clamp(activeFrame + delta, 0, Math.max(0, playbackFrames(sample).length - 1));
  renderSample();
}

function animate(now) {
  if (playing) {
    const interval = 900 / Number(speed.value || 1);
    if (!lastTick || now - lastTick >= interval) {
      const sample = currentSample();
      const frames = playbackFrames(sample);
      if (sample && activeFrame < frames.length - 1) {
        activeFrame++;
      } else {
        playing = false;
      }
      lastTick = now;
      renderSample();
    }
  }
  requestAnimationFrame(animate);
}

function currentSample() {
  return DATA.samples.find(s => s.id === activeId) || DATA.samples[0];
}

function playbackFrames(sample) {
  if (!sample) return [];
  return sample.visualFrames && sample.visualFrames.length ? sample.visualFrames : sample.timeline;
}

function currentVisualFrame(sample) {
  if (!sample || !sample.visualFrames || !sample.visualFrames.length) return null;
  return sample.visualFrames[activeFrame] || null;
}

function activeLogIndex(sample) {
  return sample.timeline[activeFrame]?.logIndex ?? -1;
}

function frameByLogIndex(sample, logIndex) {
  const frame = sample.timeline.findIndex(x => x.logIndex === logIndex);
  return frame < 0 ? activeFrame : frame;
}

function buildBadges(values) {
  return values.filter(v => v !== undefined && v !== null && String(v).trim().length > 0)
    .map(v => '<span class=""badge"">' + escapeHtml(v) + '</span>')
    .join('');
}

function formatEntry(entry) {
  if (entry.kind === 'Line') return '&nbsp;';
  if (entry.kind === 'Divider') return '------------------------------------------------------------------------';
  if (entry.kind === 'Bullet') return '  - ' + escapeHtml(entry.text);
  if (entry.kind === 'Numbered') return '  ' + entry.number + '. ' + escapeHtml(entry.text);
  if (entry.kind === 'KeyValue') return '<span class=""key"">' + escapeHtml(entry.key) + ':</span> ' + escapeHtml(entry.text);
  return escapeHtml(entry.text);
}

function kindClass(kind) {
  if (kind === 'Section') return 'section';
  if (kind === 'Warn') return 'warn';
  if (kind === 'Error') return 'error';
  if (kind === 'KeyValue') return 'kv';
  return '';
}

function wrapCanvasText(text, x, y, maxWidth, lineHeight, maxLines) {
  const words = String(text || '').split(/\s+/);
  let line = '';
  let lines = 0;
  for (const word of words) {
    const test = line ? line + ' ' + word : word;
    if (ctx.measureText(test).width > maxWidth && line) {
      ctx.fillText(line, x, y + lines * lineHeight);
      line = word;
      lines++;
      if (lines >= maxLines) return;
    } else {
      line = test;
    }
  }
  if (line && lines < maxLines) ctx.fillText(line, x, y + lines * lineHeight);
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function escapeHtml(value) {
  const entity = String.fromCharCode(38);
  return String(value ?? '')
    .replace(/&/g, entity + 'amp;')
    .replace(/</g, entity + 'lt;')
    .replace(/>/g, entity + 'gt;')
    .replace(/""/g, entity + 'quot;')
    .replace(/'/g, entity + '#39;');
}

document.addEventListener('click', event => {
  const moduleButton = event.target.closest('[data-module-filter]');
  if (moduleButton) {
    const module = moduleButton.getAttribute('data-module-filter');
    if (!module) return;
    search.value = search.value.trim().toLowerCase() === module.toLowerCase() ? '' : module;
    renderCapabilityMap();
    renderList();
    return;
  }

  const button = event.target.closest('[data-sample-id]');
  if (!button) return;
  const id = button.getAttribute('data-sample-id');
  if (!id) return;
  activeId = id;
  activeFrame = 0;
  playing = false;
  renderCapabilityMap();
  renderList();
  renderSample();
});

search.oninput = () => { renderCapabilityMap(); renderList(); };
timeline.oninput = () => { activeFrame = Number(timeline.value); renderSample(); };
playButton.onclick = () => { playing = !playing; lastTick = 0; renderSample(); };
prevButton.onclick = () => step(-1);
nextButton.onclick = () => step(1);
requestAnimationFrame(animate);
renderCapabilityMap();
renderList();
renderSample();
</script>
</body>
</html>";
        }

        private sealed class WebSampleDocument
        {
            public string GeneratedAt { get; set; } = string.Empty;
            public string OutputSchemaVersion { get; set; } = string.Empty;
            public List<WebSampleEntry> Samples { get; set; } = new();
        }

        private sealed class WebSampleEntry
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string[] Tags { get; set; } = Array.Empty<string>();
            public string Status { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
            public string[] Modules { get; set; } = Array.Empty<string>();
            public string[] Next { get; set; } = Array.Empty<string>();
            public SampleGuideContent Guide { get; set; } = new();
            public WebCodeWalkthroughStep[] CodeWalkthrough { get; set; } = Array.Empty<WebCodeWalkthroughStep>();
            public SampleLearningContract LearningContract { get; set; } = new();
            public SampleVisualFrame[] VisualFrames { get; set; } = Array.Empty<SampleVisualFrame>();
            public SampleInputField[] InputFields { get; set; } = Array.Empty<SampleInputField>();
            public SampleLearningCheckpoint[] LearningCheckpoints { get; set; } = Array.Empty<SampleLearningCheckpoint>();
            public string VisualTemplate { get; set; } = string.Empty;
            public SampleVisualModel VisualModel { get; set; } = new();
            public bool Succeeded { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public List<WebLogEntry> Logs { get; set; } = new();
            public List<WebTimelineEvent> Timeline { get; set; } = new();

            public static WebSampleEntry From(SampleCatalogEntry entry, IReadOnlyList<SampleLogEntry> logs, SampleExecutionResult result)
            {
                return new WebSampleEntry
                {
                    Id = entry.Id,
                    Title = entry.Title,
                    Description = entry.Description,
                    Category = entry.Category.GetDisplayName(),
                    Tags = entry.Tags.ToArray(),
                    Status = entry.Status,
                    Level = entry.Level,
                    Modules = entry.Modules.ToArray(),
                    Next = entry.Next.ToArray(),
                    Guide = entry.Guide,
                    CodeWalkthrough = entry.CodeWalkthrough.Select(WebCodeWalkthroughStep.From).ToArray(),
                    LearningContract = entry.LearningContract,
                    VisualFrames = entry.VisualFrames.ToArray(),
                    InputFields = entry.InputFields.ToArray(),
                    LearningCheckpoints = entry.LearningCheckpoints.ToArray(),
                    VisualTemplate = entry.VisualTemplate,
                    VisualModel = entry.VisualModel,
                    Succeeded = result.Succeeded,
                    ErrorMessage = result.ErrorMessage,
                    Logs = logs.Select(WebLogEntry.From).ToList(),
                    Timeline = logs.Select(WebTimelineEvent.From).ToList()
                };
            }
        }

        private sealed class WebCodeWalkthroughStep
        {
            public string Title { get; set; } = string.Empty;
            public string SourceFile { get; set; } = string.Empty;
            public int StartLine { get; set; }
            public int EndLine { get; set; }
            public string Explanation { get; set; } = string.Empty;
            public string OutputHint { get; set; } = string.Empty;
            public string VisualStep { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;

            public static WebCodeWalkthroughStep From(SampleCodeWalkthroughStep step)
            {
                return new WebCodeWalkthroughStep
                {
                    Title = step.Title,
                    SourceFile = step.SourceFile,
                    StartLine = step.StartLine,
                    EndLine = step.EndLine,
                    Explanation = step.Explanation,
                    OutputHint = step.OutputHint,
                    VisualStep = step.VisualStep,
                    Code = ReadCodeExcerpt(step.SourceFile, step.StartLine, step.EndLine)
                };
            }
        }

        private static string ReadCodeExcerpt(string sourceFile, int startLine, int endLine)
        {
            if (string.IsNullOrWhiteSpace(sourceFile) || startLine <= 0 || endLine < startLine)
                return string.Empty;

            var path = ResolveSourceFile(sourceFile);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return string.Empty;

            var lines = File.ReadAllLines(path);
            var start = Math.Max(0, startLine - 1);
            var end = Math.Min(lines.Length - 1, endLine - 1);
            if (start > end)
                return string.Empty;

            return string.Join(Environment.NewLine, lines.Skip(start).Take(end - start + 1));
        }

        private static string ResolveSourceFile(string sourceFile)
        {
            if (Path.IsPathRooted(sourceFile) && File.Exists(sourceFile))
                return sourceFile;

            var current = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                var candidate = Path.GetFullPath(Path.Combine(current, sourceFile));
                if (File.Exists(candidate))
                    return candidate;

                var parent = Directory.GetParent(current);
                if (parent == null)
                    break;

                current = parent.FullName;
            }

            return Path.GetFullPath(sourceFile);
        }

        private sealed class WebLogEntry
        {
            public int Sequence { get; set; }
            public string Kind { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string Key { get; set; } = string.Empty;
            public int? Number { get; set; }

            public static WebLogEntry From(SampleLogEntry entry)
            {
                return new WebLogEntry
                {
                    Sequence = entry.Sequence,
                    Kind = entry.Kind.ToString(),
                    Text = entry.Text,
                    Key = entry.Key,
                    Number = entry.Number
                };
            }
        }

        private sealed class WebTimelineEvent
        {
            public int Index { get; set; }
            public double Time { get; set; }
            public string Kind { get; set; } = string.Empty;
            public string Severity { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public int LogIndex { get; set; }

            public static WebTimelineEvent From(SampleLogEntry entry, int index)
            {
                return new WebTimelineEvent
                {
                    Index = index,
                    Time = index * 0.5,
                    Kind = entry.Kind.ToString(),
                    Severity = entry.Kind.ToString(),
                    Label = CreateLabel(entry),
                    LogIndex = index
                };
            }

            private static string CreateLabel(SampleLogEntry entry)
            {
                if (entry.Kind == SampleLogKind.KeyValue && !string.IsNullOrWhiteSpace(entry.Key))
                    return $"{entry.Key}: {entry.Text}";

                if (entry.Kind == SampleLogKind.Numbered && entry.Number.HasValue)
                    return $"{entry.Number.Value}. {entry.Text}";

                if (entry.Kind == SampleLogKind.Divider)
                    return "阶段分隔";

                if (entry.Kind == SampleLogKind.Line)
                    return "空白输出";

                return entry.Text;
            }
        }
    }
}
