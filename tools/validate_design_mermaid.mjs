import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

const validationPackage = path.resolve(process.cwd(), 'artifacts/mermaid-validation/package.json');
const validationRequire = createRequire(pathToFileURL(validationPackage).href);
const { JSDOM } = validationRequire('jsdom');
const dom = new JSDOM('<!doctype html><html><body></body></html>');
globalThis.window = dom.window;
globalThis.document = dom.window.document;
globalThis.DOMParser = dom.window.DOMParser;
globalThis.Element = dom.window.Element;
Object.defineProperty(globalThis, 'navigator', {
  configurable: true,
  value: dom.window.navigator,
});

const mermaidEntry = path.resolve(process.cwd(), 'artifacts/mermaid-validation/node_modules/mermaid/dist/mermaid.esm.mjs');
if (!fs.existsSync(mermaidEntry)) {
  throw new Error('Mermaid dependency missing. Run: npm install --prefix artifacts\\mermaid-validation mermaid');
}
const mermaid = (await import(pathToFileURL(mermaidEntry).href)).default;

const sourceDir = process.argv[2] ?? 'Docs/design';
const root = process.cwd();
const sourceRoot = path.resolve(root, sourceDir);
const outDir = path.resolve(root, 'artifacts/mermaid-validation');
const reportPath = path.join(outDir, 'report.json');
const summaryPath = path.join(outDir, 'report.md');

function walk(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...walk(fullPath));
    } else if (entry.isFile() && entry.name.endsWith('.md')) {
      files.push(fullPath);
    }
  }
  return files.sort((a, b) => a.localeCompare(b));
}

function lineOfOffset(text, offset) {
  let line = 1;
  for (let i = 0; i < offset; i++) {
    if (text.charCodeAt(i) === 10) line++;
  }
  return line;
}

function extractMermaidBlocks(filePath) {
  const text = fs.readFileSync(filePath, 'utf8');
  const regex = /(^|\r?\n)```mermaid\s*\r?\n([\s\S]*?)\r?\n```/g;
  const blocks = [];
  let match;
  while ((match = regex.exec(text)) !== null) {
    const fenceOffset = match.index + match[1].length;
    blocks.push({
      filePath,
      startLine: lineOfOffset(text, fenceOffset),
      content: match[2].trim(),
    });
  }
  return blocks;
}

function normalizeError(error) {
  if (!error) return 'Unknown Mermaid parse error';
  if (typeof error === 'string') return error;
  if (error.str) return String(error.str);
  if (error.message) return String(error.message);
  return JSON.stringify(error);
}

function firstLine(text) {
  return text.split(/\r?\n/, 1)[0] ?? '';
}

mermaid.initialize({
  startOnLoad: false,
  securityLevel: 'loose',
  flowchart: { htmlLabels: false },
  sequence: { useMaxWidth: false },
});

const files = walk(sourceRoot);
const blocks = files.flatMap(extractMermaidBlocks);
const failures = [];

for (const block of blocks) {
  try {
    await mermaid.parse(block.content);
  } catch (error) {
    failures.push({
      source: path.relative(root, block.filePath).replaceAll('\\', '/'),
      line: block.startLine,
      diagramType: firstLine(block.content),
      error: normalizeError(error),
    });
  }
}

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(reportPath, JSON.stringify({ sourceDir, totalBlocks: blocks.length, failures }, null, 2), 'utf8');

const lines = [
  '# Mermaid validation report',
  '',
  `SourceDir: ${sourceDir}`,
  `TotalBlocks: ${blocks.length}`,
  `Failures: ${failures.length}`,
  '',
  '| File | Line | Diagram | Error |',
  '|------|------|---------|-------|',
];
for (const failure of failures) {
  const error = failure.error.replace(/\r?\n/g, '<br>').replace(/\|/g, '\\|');
  lines.push(`| ${failure.source} | ${failure.line} | ${failure.diagramType.replace(/\|/g, '\\|')} | ${error} |`);
}
fs.writeFileSync(summaryPath, `${lines.join('\n')}\n`, 'utf8');

console.log(`Mermaid blocks: ${blocks.length}`);
console.log(`Failures: ${failures.length}`);
console.log(`Report: ${path.relative(root, summaryPath).replaceAll('\\', '/')}`);

if (failures.length > 0) {
  process.exitCode = 1;
}
