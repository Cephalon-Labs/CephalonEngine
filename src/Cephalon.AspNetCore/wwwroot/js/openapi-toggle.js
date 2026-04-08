const baseTitle = "Cephalon REST API";
const hashSectionRoots = new Set(["description", "model", "operation", "tag", "webhook"]);

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
  const match = String(pathname ?? "").match(/^\/scalar\/([^/?#]+)/i);
  return match ? decodeURIComponent(match[1]).trim() : "";
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

function isHashSectionRoot(value) {
  return hashSectionRoots.has(String(value ?? "").trim().toLowerCase());
}

function resolveCanonicalUrl(preferredDocumentName = "") {
  if (typeof window === "undefined" || typeof window.history === "undefined") {
    return "";
  }

  const { pathname, search, hash } = window.location;
  const currentDocumentName = getPathDocumentName(pathname);
  const supportsScalarDocumentPath =
    pathname === "/scalar" ||
    pathname === "/scalar/" ||
    currentDocumentName.length > 0;

  if (!supportsScalarDocumentPath) {
    return "";
  }

  let documentName = String(preferredDocumentName ?? "").trim() || currentDocumentName;
  let hashSegments = splitHashSegments(hash);

  if (hashSegments.length > 0) {
    const [firstSegment, secondSegment] = hashSegments;
    const hashRepeatsCurrentDocument =
      currentDocumentName.length > 0 &&
      equalsIgnoreCase(firstSegment, currentDocumentName) &&
      (hashSegments.length === 1 || isHashSectionRoot(secondSegment));
    const hashCarriesVersionDocument =
      isVersionDocumentName(firstSegment) &&
      (hashSegments.length === 1 || isHashSectionRoot(secondSegment));

    if (hashRepeatsCurrentDocument || hashCarriesVersionDocument) {
      documentName = firstSegment;
      hashSegments = hashSegments.slice(1);
    }
  }

  if (documentName.length === 0) {
    return "";
  }

  const nextPath = `/scalar/${encodeURIComponent(documentName)}`;
  const nextHash = hashSegments.length > 0 ? `#${hashSegments.join("/")}` : "";
  const nextUrl = `${nextPath}${search ?? ""}${nextHash}`;
  const currentUrl = `${pathname}${search ?? ""}${hash ?? ""}`;
  return currentUrl === nextUrl ? "" : nextUrl;
}

function normalizeHashCanonicalUrl() {
  const nextUrl = resolveCanonicalUrl();
  if (!nextUrl) {
    return;
  }

  window.history.replaceState(window.history.state, "", nextUrl);
}

function scheduleHashCanonicalization() {
  if (typeof window === "undefined") {
    return;
  }

  normalizeHashCanonicalUrl();
  window.setTimeout(normalizeHashCanonicalUrl, 0);
  window.setTimeout(normalizeHashCanonicalUrl, 250);
}

if (typeof window !== "undefined") {
  window.addEventListener("hashchange", normalizeHashCanonicalUrl);
}

scheduleHashCanonicalization();

export default {
  generateOperationSlug: (operation) => {
    const method = toSlug(operation?.method);
    const path = toSlug(operation?.path);
    return `cephalon-${method}-${path}`;
  },
  onDocumentSelect: (selectedDocument) => {
    updateTitle(selectedDocument);
    updateCanonicalUrl(selectedDocument);
  },
};
