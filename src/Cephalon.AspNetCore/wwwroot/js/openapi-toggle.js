const baseTitle = "Cephalon REST API";
const configuredScalarRoutePrefix = "__CEPHALON_SCALAR_ROUTE_PREFIX__";
const configuredDocumentNames = __CEPHALON_SCALAR_DOCUMENT_NAMES__;
const configuredDefaultDocumentName = __CEPHALON_SCALAR_DEFAULT_DOCUMENT_NAME__;
const hashSectionRoots = new Set(["description", "model", "operation", "tag", "webhook"]);
const scalarRoutePrefix = normalizeRoutePrefix(configuredScalarRoutePrefix);
const documentNames = normalizeDocumentNames(configuredDocumentNames);
const defaultDocumentName = resolveDefaultDocumentName(configuredDefaultDocumentName, documentNames);
const selectorShellId = "cephalon-scalar-document-selector-shell";
const selectorId = "cephalon-scalar-document-selector";
const selectorLabelId = "cephalon-scalar-document-selector-label";

function normalizeRoutePrefix(value) {
  const normalized = String(value ?? "").trim();
  if (normalized.length === 0 || normalized === "/") {
    return "/scalar";
  }

  const withLeadingSlash = normalized.startsWith("/") ? normalized : `/${normalized}`;
  return withLeadingSlash.length > 1
    ? withLeadingSlash.replace(/\/+$/, "")
    : withLeadingSlash;
}

function normalizeDocumentNames(values) {
  if (!Array.isArray(values)) {
    return [];
  }

  return values
    .map((value) => String(value ?? "").trim())
    .filter((value) => value.length > 0)
    .filter((value, index, items) =>
      items.findIndex((candidate) => equalsIgnoreCase(candidate, value)) === index);
}

function resolveDefaultDocumentName(value, availableDocumentNames) {
  const normalized = String(value ?? "").trim();
  if (normalized.length > 0) {
    return normalized;
  }

  return availableDocumentNames.length > 0
    ? availableDocumentNames[0]
    : "";
}

function escapeRegex(value) {
  return String(value ?? "").replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function toSlug(value) {
  return String(value ?? "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function updateTitle(selectedDocument) {
  if (typeof document === "undefined") {
    return;
  }

  const documentTitle =
    selectedDocument && typeof selectedDocument.title === "string"
      ? selectedDocument.title.trim()
      : "";

  document.title = documentTitle.length > 0
    ? `${baseTitle} | ${documentTitle}`
    : baseTitle;
}

function updateCanonicalUrl(selectedDocument) {
  if (typeof window === "undefined" || typeof window.history === "undefined") {
    return;
  }

  const nextUrl = resolveCanonicalUrl(
    selectedDocument && typeof selectedDocument.title === "string"
      ? selectedDocument.title.trim()
      : "");

  if (!nextUrl) {
    return;
  }

  window.history.replaceState(window.history.state, "", nextUrl);
}

function getPathDocumentName(pathname) {
  const match = String(pathname ?? "")
    .match(new RegExp(`^${escapeRegex(scalarRoutePrefix)}(?:/([^/?#]+))?$`, "i"));
  return match && match[1]
    ? decodeURIComponent(match[1]).trim()
    : "";
}

function splitHashSegments(hash) {
  const normalized = String(hash ?? "")
    .replace(/^#\/?/, "")
    .replace(/\/+$/, "")
    .trim();

  return normalized.length > 0
    ? normalized.split("/").filter((segment) => segment.length > 0)
    : [];
}

function equalsIgnoreCase(left, right) {
  return String(left ?? "").localeCompare(String(right ?? ""), undefined, { sensitivity: "accent" }) === 0;
}

function isVersionDocumentName(value) {
  return /^v[1-9]\d*$/i.test(String(value ?? "").trim());
}

function isKnownDocumentName(value) {
  return documentNames.some((documentName) => equalsIgnoreCase(documentName, value));
}

function isHashSectionRoot(value) {
  return hashSectionRoots.has(String(value ?? "").trim().toLowerCase());
}

function stripDocumentHashPrefix(hashSegments, currentDocumentName) {
  if (hashSegments.length === 0) {
    return hashSegments;
  }

  const [firstSegment, secondSegment] = hashSegments;
  const hashRepeatsCurrentDocument =
    currentDocumentName.length > 0 &&
    equalsIgnoreCase(firstSegment, currentDocumentName) &&
    (hashSegments.length === 1 || isHashSectionRoot(secondSegment));
  const hashCarriesKnownDocument =
    isKnownDocumentName(firstSegment) &&
    (hashSegments.length === 1 || isHashSectionRoot(secondSegment));

  return hashRepeatsCurrentDocument || hashCarriesKnownDocument
    ? hashSegments.slice(1)
    : hashSegments;
}

function resolveCanonicalUrl(preferredDocumentName = "", allowHashDocumentOverride = preferredDocumentName.length === 0) {
  if (typeof window === "undefined" || typeof window.history === "undefined") {
    return "";
  }

  const { pathname, search, hash } = window.location;
  const currentDocumentName = getPathDocumentName(pathname);
  const isScalarRootPath = pathname.localeCompare(scalarRoutePrefix, undefined, { sensitivity: "accent" }) === 0;
  const isScalarShellPath = pathname.localeCompare(`${scalarRoutePrefix}/`, undefined, { sensitivity: "accent" }) === 0;
  const supportsScalarDocumentPath =
    isScalarRootPath ||
    isScalarShellPath ||
    currentDocumentName.length > 0;

  if (!supportsScalarDocumentPath) {
    return "";
  }

  let documentName = String(preferredDocumentName ?? "").trim() || currentDocumentName || defaultDocumentName;
  let hashSegments = splitHashSegments(hash);

  if (hashSegments.length > 0) {
    const [firstSegment] = hashSegments;
    const normalizedHashSegments = stripDocumentHashPrefix(hashSegments, currentDocumentName);

    if (normalizedHashSegments.length !== hashSegments.length) {
      if (allowHashDocumentOverride) {
        documentName = firstSegment;
      }

      hashSegments = normalizedHashSegments;
    }
  }

  if (documentName.length === 0) {
    return "";
  }

  const nextPath = `${scalarRoutePrefix}/${encodeURIComponent(documentName)}`;
  const nextHash = hashSegments.length > 0 ? `#${hashSegments.join("/")}` : "";
  const nextUrl = `${nextPath}${search ?? ""}${nextHash}`;
  const currentUrl = `${pathname}${search ?? ""}${hash ?? ""}`;
  return currentUrl === nextUrl ? "" : nextUrl;
}

function normalizeHashCanonicalUrl() {
  const nextUrl = resolveCanonicalUrl();
  if (!nextUrl) {
    syncSelectorValue();
    return;
  }

  window.history.replaceState(window.history.state, "", nextUrl);
  syncSelectorValue();
}

function scheduleHashCanonicalization() {
  if (typeof window === "undefined") {
    return;
  }

  normalizeHashCanonicalUrl();
  window.setTimeout(normalizeHashCanonicalUrl, 0);
  window.setTimeout(normalizeHashCanonicalUrl, 250);
}

function getActiveDocumentName() {
  if (typeof window === "undefined") {
    return defaultDocumentName;
  }

  const currentDocumentName = getPathDocumentName(window.location.pathname);
  if (currentDocumentName.length > 0) {
    return currentDocumentName;
  }

  return defaultDocumentName;
}

function useVersionLabel() {
  return documentNames.length > 0 && documentNames.every((documentName) => isVersionDocumentName(documentName));
}

function buildSelectorTargetUrl(documentName) {
  const preferredDocumentName = String(documentName ?? "").trim();
  if (preferredDocumentName.length === 0) {
    return "";
  }

  const canonicalUrl = resolveCanonicalUrl(preferredDocumentName, false);
  if (canonicalUrl.length > 0) {
    return canonicalUrl;
  }

  if (typeof window === "undefined") {
    return "";
  }

  const hashSegments = stripDocumentHashPrefix(
    splitHashSegments(window.location.hash),
    getPathDocumentName(window.location.pathname));
  const nextHash = hashSegments.length > 0 ? `#${hashSegments.join("/")}` : "";
  return `${scalarRoutePrefix}/${encodeURIComponent(preferredDocumentName)}${window.location.search ?? ""}${nextHash}`;
}

function syncSelectorValue() {
  if (typeof document === "undefined") {
    return;
  }

  const selector = document.getElementById(selectorId);
  if (!selector || selector.tagName !== "SELECT") {
    return;
  }

  const activeDocumentName = getActiveDocumentName();
  if (activeDocumentName.length > 0 && selector.value !== activeDocumentName) {
    selector.value = activeDocumentName;
  }
}

function navigateToSelectedDocument(documentName) {
  if (typeof window === "undefined") {
    return;
  }

  const nextUrl = buildSelectorTargetUrl(documentName);
  if (nextUrl.length === 0) {
    return;
  }

  const currentUrl = `${window.location.pathname}${window.location.search ?? ""}${window.location.hash ?? ""}`;
  if (currentUrl === nextUrl) {
    syncSelectorValue();
    return;
  }

  window.location.assign(nextUrl);
}

function createSelectorOption(documentName) {
  const option = document.createElement("option");
  option.value = documentName;
  option.textContent = documentName;
  return option;
}

function ensureVersionSelector() {
  if (typeof document === "undefined" || documentNames.length <= 1 || !document.body) {
    return;
  }

  let shell = document.getElementById(selectorShellId);
  if (!shell) {
    shell = document.createElement("div");
    shell.id = selectorShellId;
    shell.style.position = "fixed";
    shell.style.top = "0.75rem";
    shell.style.right = "0.75rem";
    shell.style.zIndex = "2147483647";
    shell.style.display = "flex";
    shell.style.alignItems = "center";
    shell.style.gap = "0.5rem";
    shell.style.padding = "0.5rem 0.75rem";
    shell.style.border = "1px solid rgba(15, 23, 42, 0.14)";
    shell.style.borderRadius = "999px";
    shell.style.background = "rgba(255, 255, 255, 0.96)";
    shell.style.boxShadow = "0 10px 30px rgba(15, 23, 42, 0.14)";
    shell.style.backdropFilter = "blur(10px)";
    shell.style.fontFamily = "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif";
    shell.style.fontSize = "0.875rem";
    shell.style.color = "#0f172a";

    const label = document.createElement("label");
    label.id = selectorLabelId;
    label.htmlFor = selectorId;
    label.textContent = useVersionLabel() ? "Version" : "Document";
    label.style.fontWeight = "600";
    label.style.whiteSpace = "nowrap";

    const selector = document.createElement("select");
    selector.id = selectorId;
    selector.setAttribute("aria-labelledby", selectorLabelId);
    selector.style.border = "1px solid rgba(15, 23, 42, 0.14)";
    selector.style.borderRadius = "999px";
    selector.style.padding = "0.35rem 0.75rem";
    selector.style.background = "#ffffff";
    selector.style.color = "#0f172a";
    selector.style.font = "inherit";
    selector.style.maxWidth = "9rem";
    selector.style.cursor = "pointer";
    selector.addEventListener("change", (event) => {
      navigateToSelectedDocument(event.target && typeof event.target.value === "string"
        ? event.target.value
        : "");
    });

    shell.appendChild(label);
    shell.appendChild(selector);
    document.body.appendChild(shell);
  }

  const label = document.getElementById(selectorLabelId);
  if (label) {
    label.textContent = useVersionLabel() ? "Version" : "Document";
  }

  const selector = document.getElementById(selectorId);
  if (!selector || selector.tagName !== "SELECT") {
    return;
  }

  if (selector.options.length !== documentNames.length ||
      documentNames.some((documentName, index) => selector.options[index]?.value !== documentName)) {
    selector.replaceChildren(...documentNames.map((documentName) => createSelectorOption(documentName)));
  }

  syncSelectorValue();
}

function scheduleSelectorRefresh() {
  if (typeof window === "undefined" || documentNames.length <= 1) {
    return;
  }

  ensureVersionSelector();
  window.setTimeout(ensureVersionSelector, 0);
  window.setTimeout(ensureVersionSelector, 250);
  window.setTimeout(ensureVersionSelector, 1000);
}

if (typeof window !== "undefined") {
  window.addEventListener("hashchange", normalizeHashCanonicalUrl);
}

scheduleHashCanonicalization();
scheduleSelectorRefresh();

if (typeof document !== "undefined" && document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", scheduleSelectorRefresh, { once: true });
}

export default {
  generateOperationSlug: (operation) => {
    const method = toSlug(operation?.method);
    const path = toSlug(operation?.path);
    return `cephalon-${method}-${path}`;
  },
  onDocumentSelect: (selectedDocument) => {
    updateTitle(selectedDocument);
    updateCanonicalUrl(selectedDocument);
    scheduleSelectorRefresh();
  },
};
