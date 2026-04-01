namespace Cephalon.ReferenceDocs.Generation;

internal static class ReferenceBrowserRenderer
{
    internal static string BuildPage(string manifestJson)
    {
        ArgumentNullException.ThrowIfNull(manifestJson);

        return """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Cephalon Reference Browser</title>
  <link rel="stylesheet" href="reference-browser.css">
</head>
<body>
  <div class="page-shell">
    <header class="hero">
      <p class="eyebrow">Cephalon Reference Docs</p>
      <h1>Reference Browser</h1>
      <p class="hero-copy">
        Explore the published API surface by type, member, namespace, and assembly without scanning long Markdown pages by hand.
      </p>
      <nav class="hero-links">
        <a href="README.md">Reference Home</a>
        <a href="namespaces.md">Namespace Index</a>
        <a href="types.md">Type Index</a>
        <a href="members.md">Member Index</a>
        <a href="reference-manifest.json">JSON Manifest</a>
      </nav>
    </header>

    <main class="layout">
      <section class="controls">
        <label class="control">
          <span>Search scope</span>
          <select id="scope-filter">
            <option value="types">Types</option>
            <option value="members">Members</option>
          </select>
        </label>

        <label class="control">
          <span>Search</span>
          <input id="search-box" type="search" placeholder="EngineBuilder, AddCephalon, Runtime..." autocomplete="off">
        </label>

        <label class="control">
          <span>Category</span>
          <select id="category-filter">
            <option value="">All categories</option>
          </select>
        </label>

        <label class="control">
          <span>Assembly</span>
          <select id="assembly-filter">
            <option value="">All assemblies</option>
          </select>
        </label>

        <label class="control">
          <span>Namespace</span>
          <select id="namespace-filter">
            <option value="">All namespaces</option>
          </select>
        </label>

        <div class="control action-control">
          <span>Filters</span>
          <button id="clear-filters" type="button">Clear Filters</button>
        </div>
      </section>

      <section class="summary-grid" id="summary-grid">
        <article class="summary-card">
          <span class="summary-label">Assemblies</span>
          <strong id="assembly-count">0</strong>
        </article>
        <article class="summary-card">
          <span class="summary-label">Namespaces</span>
          <strong id="namespace-count">0</strong>
        </article>
        <article class="summary-card">
          <span class="summary-label">Public Types</span>
          <strong id="type-count">0</strong>
        </article>
        <article class="summary-card">
          <span class="summary-label">Public Members</span>
          <strong id="member-count">0</strong>
        </article>
        <article class="summary-card">
          <span class="summary-label">Visible Results</span>
          <strong id="visible-count">0</strong>
        </article>
      </section>

      <section class="result-panel">
        <div class="result-panel-header">
          <div>
            <p class="eyebrow" id="result-eyebrow">Type Results</p>
            <h2 id="result-title">Public Types</h2>
          </div>
          <p class="result-hint" id="result-hint">Results link back to the generated Markdown pages and deep anchors.</p>
        </div>
        <div id="result-list" class="result-list"></div>
      </section>
    </main>
  </div>

  <script id="reference-manifest" type="application/json">__INLINE_MANIFEST__</script>
  <script src="reference-browser.js"></script>
</body>
</html>
""".Replace("__INLINE_MANIFEST__", EscapeInlineJson(manifestJson), StringComparison.Ordinal);
    }

    internal static string BuildStyles()
    {
        return """
:root {
  --page-background: linear-gradient(180deg, #f8f5ef 0%, #f4efe6 55%, #ede6d8 100%);
  --surface: rgba(255, 252, 246, 0.9);
  --surface-strong: #fffaf0;
  --border: rgba(107, 87, 54, 0.18);
  --text-primary: #1f1a14;
  --text-secondary: #5a4b37;
  --accent: #8a4f12;
  --accent-soft: rgba(138, 79, 18, 0.12);
  --shadow: 0 18px 48px rgba(63, 47, 25, 0.12);
  --radius-lg: 22px;
  --radius-md: 16px;
  --radius-sm: 12px;
}

* {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: "Segoe UI Variable", "Segoe UI", system-ui, sans-serif;
  color: var(--text-primary);
  background: var(--page-background);
}

a {
  color: var(--accent);
}

.page-shell {
  width: min(1200px, calc(100vw - 32px));
  margin: 0 auto;
  padding: 32px 0 48px;
}

.hero {
  background: radial-gradient(circle at top left, rgba(184, 111, 24, 0.16), transparent 38%), var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow);
  padding: 28px;
}

.eyebrow {
  margin: 0 0 8px;
  text-transform: uppercase;
  letter-spacing: 0.14em;
  font-size: 0.74rem;
  color: var(--text-secondary);
}

.hero h1,
.result-panel h2 {
  margin: 0;
  font-family: Georgia, "Times New Roman", serif;
  font-weight: 600;
  letter-spacing: -0.03em;
}

.hero h1 {
  font-size: clamp(2rem, 4vw, 3.2rem);
}

.hero-copy {
  max-width: 760px;
  color: var(--text-secondary);
  line-height: 1.65;
}

.hero-links {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 18px;
}

.hero-links a {
  text-decoration: none;
  background: var(--surface-strong);
  border: 1px solid var(--border);
  border-radius: 999px;
  padding: 10px 14px;
}

.layout {
  display: grid;
  gap: 20px;
  margin-top: 22px;
}

.controls,
.summary-grid,
.result-panel {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow);
}

.controls {
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  padding: 20px;
}

.control {
  display: grid;
  gap: 8px;
}

.control span {
  font-size: 0.92rem;
  color: var(--text-secondary);
}

.control input,
.control select {
  width: 100%;
  padding: 12px 14px;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border);
  background: rgba(255, 255, 255, 0.9);
  color: var(--text-primary);
  font: inherit;
}

.action-control {
  align-content: end;
}

.action-control button {
  width: 100%;
  padding: 12px 14px;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border);
  background: var(--surface-strong);
  color: var(--accent);
  font: inherit;
  cursor: pointer;
}

.action-control button:hover {
  background: #fff6e8;
}

.summary-grid {
  display: grid;
  gap: 14px;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  padding: 20px;
}

.summary-card {
  background: var(--surface-strong);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  padding: 16px;
}

.summary-label {
  display: block;
  color: var(--text-secondary);
  font-size: 0.88rem;
  margin-bottom: 8px;
}

.summary-card strong {
  font-size: 1.8rem;
  font-weight: 650;
}

.result-panel {
  padding: 22px;
}

.result-panel-header {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: end;
  justify-content: space-between;
}

.result-hint {
  margin: 0;
  color: var(--text-secondary);
}

.result-list {
  display: grid;
  gap: 14px;
  margin-top: 20px;
}

.result-card {
  background: var(--surface-strong);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  padding: 18px;
}

.result-card h3 {
  margin: 0;
  font-size: 1.1rem;
}

.result-card h3 a {
  text-decoration: none;
}

.result-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin: 10px 0 0;
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border-radius: 999px;
  padding: 6px 10px;
  background: var(--accent-soft);
  color: var(--accent);
  font-size: 0.82rem;
}

.result-summary {
  margin: 14px 0 0;
  color: var(--text-secondary);
  line-height: 1.6;
}

.result-declaration {
  margin: 14px 0 0;
  overflow-x: auto;
  border-radius: var(--radius-sm);
  background: #1c1915;
  color: #f9efe2;
  padding: 12px 14px;
  font-size: 0.9rem;
}

.member-grid {
  display: grid;
  gap: 8px;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  margin-top: 14px;
}

.member-stat {
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 10px 12px;
  background: rgba(255, 255, 255, 0.6);
}

.member-stat span {
  display: block;
  color: var(--text-secondary);
  font-size: 0.8rem;
}

.member-stat strong {
  font-size: 1.15rem;
}

.empty-state {
  padding: 28px;
  text-align: center;
  color: var(--text-secondary);
  border: 1px dashed var(--border);
  border-radius: var(--radius-md);
  background: rgba(255, 255, 255, 0.45);
}

@media (max-width: 720px) {
  .page-shell {
    width: min(100vw - 20px, 1200px);
    padding-top: 20px;
  }

  .hero,
  .controls,
  .summary-grid,
  .result-panel {
    padding: 18px;
  }
}
""";
    }

    internal static string BuildScript()
    {
        return """
const manifestElement = document.getElementById("reference-manifest");
const scopeFilter = document.getElementById("scope-filter");
const searchBox = document.getElementById("search-box");
const categoryFilter = document.getElementById("category-filter");
const assemblyFilter = document.getElementById("assembly-filter");
const namespaceFilter = document.getElementById("namespace-filter");
const clearFiltersButton = document.getElementById("clear-filters");
const resultEyebrow = document.getElementById("result-eyebrow");
const resultTitle = document.getElementById("result-title");
const resultHint = document.getElementById("result-hint");
const resultList = document.getElementById("result-list");
const assemblyCount = document.getElementById("assembly-count");
const namespaceCount = document.getElementById("namespace-count");
const typeCount = document.getElementById("type-count");
const memberCount = document.getElementById("member-count");
const visibleCount = document.getElementById("visible-count");

const manifest = JSON.parse(manifestElement.textContent || "{}");
const assemblies = manifest.Assemblies || [];
const namespaces = manifest.Namespaces || [];
const types = manifest.Types || [];
const members = manifest.Members || [];

assemblyCount.textContent = String(assemblies.length);
namespaceCount.textContent = String(namespaces.length);
typeCount.textContent = String(types.length);
memberCount.textContent = String(members.length);

populateSelect(
  categoryFilter,
  [...new Set(assemblies.map(assembly => assembly.Category).filter(Boolean))].sort((left, right) => left.localeCompare(right))
);

populateSelect(
  assemblyFilter,
  assemblies.map(assembly => assembly.AssemblyName).sort((left, right) => left.localeCompare(right))
);

populateSelect(
  namespaceFilter,
  namespaces.map(namespaceEntry => namespaceEntry.NamespaceName).sort((left, right) => left.localeCompare(right))
);

applyInitialState();

scopeFilter.addEventListener("change", render);
searchBox.addEventListener("input", render);
categoryFilter.addEventListener("change", render);
assemblyFilter.addEventListener("change", render);
namespaceFilter.addEventListener("change", render);
clearFiltersButton.addEventListener("click", clearFilters);

render();

function populateSelect(select, values) {
  for (const value of values) {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = value;
    select.append(option);
  }
}

function render() {
  const scope = scopeFilter.value || "types";
  const query = (searchBox.value || "").trim().toLowerCase();
  const category = categoryFilter.value;
  const assemblyName = assemblyFilter.value;
  const namespaceName = namespaceFilter.value;

  syncLocationState();
  resultList.replaceChildren();

  if (scope === "members") {
    const filteredMembers = members.filter(member => matchesFilters(member, query, category, assemblyName, namespaceName, true));
    updateResultPanel(scope, filteredMembers.length);

    if (filteredMembers.length === 0) {
      renderEmptyState("No public members match the current filters.");
      return;
    }

    for (const member of filteredMembers) {
      resultList.append(renderMemberCard(member));
    }

    return;
  }

  const filteredTypes = types.filter(type => matchesFilters(type, query, category, assemblyName, namespaceName, false));
  updateResultPanel(scope, filteredTypes.length);

  if (filteredTypes.length === 0) {
    renderEmptyState("No public types match the current filters.");
    return;
  }

  for (const type of filteredTypes) {
    resultList.append(renderTypeCard(type));
  }
}

function matchesFilters(entry, query, category, assemblyName, namespaceName, isMember) {
  const assembly = assemblies.find(candidate => candidate.AssemblyName === entry.AssemblyName);
  const matchesCategory = !category || (assembly && assembly.Category === category);
  const matchesAssembly = !assemblyName || entry.AssemblyName === assemblyName;
  const matchesNamespace = !namespaceName || entry.NamespaceName === namespaceName;
  const haystack = isMember
    ? [
        entry.DisplayName,
        entry.DeclaringTypeName,
        entry.NamespaceName,
        entry.AssemblyName,
        entry.Category,
        entry.Summary || "",
        entry.Signature
      ].join(" ").toLowerCase()
    : [
        entry.DisplayName,
        entry.NamespaceName,
        entry.AssemblyName,
        entry.Summary || "",
        entry.Declaration
      ].join(" ").toLowerCase();

  return matchesCategory && matchesAssembly && matchesNamespace && (!query || haystack.includes(query));
}

function updateResultPanel(scope, count) {
  visibleCount.textContent = String(count);

  if (scope === "members") {
    resultEyebrow.textContent = "Member Results";
    resultTitle.textContent = "Public Members";
    resultHint.textContent = "Results link back to the generated Markdown member anchors and signatures.";
    return;
  }

  resultEyebrow.textContent = "Type Results";
  resultTitle.textContent = "Public Types";
  resultHint.textContent = "Results link back to the generated Markdown pages and deep anchors.";
}

function renderEmptyState(message) {
  const emptyState = document.createElement("div");
  emptyState.className = "empty-state";
  emptyState.textContent = message;
  resultList.append(emptyState);
}

function applyInitialState() {
  const params = new URLSearchParams(window.location.search);
  scopeFilter.value = params.get("scope") || "types";
  searchBox.value = params.get("q") || "";
  categoryFilter.value = params.get("category") || "";
  assemblyFilter.value = params.get("assembly") || "";
  namespaceFilter.value = params.get("namespace") || "";
}

function clearFilters() {
  scopeFilter.value = "types";
  searchBox.value = "";
  categoryFilter.value = "";
  assemblyFilter.value = "";
  namespaceFilter.value = "";
  render();
}

function syncLocationState() {
  const params = new URLSearchParams();
  appendParam(params, "scope", scopeFilter.value === "types" ? "" : scopeFilter.value);
  appendParam(params, "q", searchBox.value);
  appendParam(params, "category", categoryFilter.value);
  appendParam(params, "assembly", assemblyFilter.value);
  appendParam(params, "namespace", namespaceFilter.value);

  const queryString = params.toString();
  const nextUrl = queryString ? `${window.location.pathname}?${queryString}` : window.location.pathname;
  window.history.replaceState(null, "", nextUrl);
}

function appendParam(params, key, value) {
  const normalized = (value || "").trim();
  if (normalized) {
    params.set(key, normalized);
  }
}

function renderTypeCard(type) {
  const assembly = assemblies.find(candidate => candidate.AssemblyName === type.AssemblyName);
  const card = document.createElement("article");
  card.className = "result-card";

  const heading = document.createElement("h3");
  const typeLink = document.createElement("a");
  typeLink.href = `${type.FileName}#${type.AnchorId}`;
  typeLink.textContent = type.DisplayName;
  heading.append(typeLink);
  card.append(heading);

  const meta = document.createElement("div");
  meta.className = "result-meta";
  meta.append(createBadge(type.NamespaceName));
  meta.append(createBadge(type.AssemblyName));
  if (assembly && assembly.Category) {
    meta.append(createBadge(assembly.Category));
  }
  card.append(meta);

  if (type.Summary) {
    const summary = document.createElement("p");
    summary.className = "result-summary";
    summary.textContent = type.Summary;
    card.append(summary);
  }

  const declaration = document.createElement("pre");
  declaration.className = "result-declaration";
  declaration.textContent = type.Declaration;
  card.append(declaration);

  const memberGrid = document.createElement("div");
  memberGrid.className = "member-grid";
  memberGrid.append(createMemberStat("Constructors", type.MemberCounts.Constructors));
  memberGrid.append(createMemberStat("Fields", type.MemberCounts.Fields));
  memberGrid.append(createMemberStat("Properties", type.MemberCounts.Properties));
  memberGrid.append(createMemberStat("Methods", type.MemberCounts.Methods));
  card.append(memberGrid);

  return card;
}

function renderMemberCard(member) {
  const assembly = assemblies.find(candidate => candidate.AssemblyName === member.AssemblyName);
  const card = document.createElement("article");
  card.className = "result-card";

  const heading = document.createElement("h3");
  const memberLink = document.createElement("a");
  memberLink.href = `${member.FileName}#${member.AnchorId}`;
  memberLink.textContent = `${member.DeclaringTypeName}.${member.DisplayName}`;
  heading.append(memberLink);
  card.append(heading);

  const meta = document.createElement("div");
  meta.className = "result-meta";
  meta.append(createBadge(member.Category));
  meta.append(createBadge(member.NamespaceName));
  meta.append(createBadge(member.AssemblyName));
  if (assembly && assembly.Category) {
    meta.append(createBadge(assembly.Category));
  }
  card.append(meta);

  if (member.Summary) {
    const summary = document.createElement("p");
    summary.className = "result-summary";
    summary.textContent = member.Summary;
    card.append(summary);
  }

  const declaration = document.createElement("pre");
  declaration.className = "result-declaration";
  declaration.textContent = member.Signature;
  card.append(declaration);

  return card;
}

function createBadge(text) {
  const badge = document.createElement("span");
  badge.className = "badge";
  badge.textContent = text;
  return badge;
}

function createMemberStat(label, value) {
  const stat = document.createElement("div");
  stat.className = "member-stat";

  const text = document.createElement("span");
  text.textContent = label;

  const amount = document.createElement("strong");
  amount.textContent = String(value);

  stat.append(text, amount);
  return stat;
}
""";
    }

    private static string EscapeInlineJson(string manifestJson)
    {
        return manifestJson.Replace("</script>", "<\\/script>", StringComparison.OrdinalIgnoreCase);
    }
}
