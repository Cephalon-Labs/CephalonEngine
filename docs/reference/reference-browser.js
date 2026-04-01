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