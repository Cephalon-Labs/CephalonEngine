const baseTitle = "Cephalon REST API";

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

  const documentName =
    selectedDocument && typeof selectedDocument.title === "string"
      ? selectedDocument.title.trim()
      : "";

  if (documentName.length === 0) {
    return;
  }

  const nextPath = `/scalar/${encodeURIComponent(documentName)}`;
  const nextUrl = `${nextPath}${window.location.search ?? ""}`;
  const currentUrl = `${window.location.pathname}${window.location.search ?? ""}${window.location.hash ?? ""}`;

  if (currentUrl === nextUrl) {
    return;
  }

  window.history.replaceState(window.history.state, "", nextUrl);
}

function normalizeHashCanonicalUrl() {
  if (typeof window === "undefined" || typeof window.history === "undefined") {
    return;
  }

  const { pathname, search, hash } = window.location;
  if (pathname !== "/scalar" && pathname !== "/scalar/") {
    return;
  }

  const documentName = String(hash ?? "")
    .replace(/^#\/?/, "")
    .replace(/\/+$/, "")
    .trim();

  if (documentName.length === 0) {
    return;
  }

  const nextUrl = `/scalar/${encodeURIComponent(documentName)}${search ?? ""}`;
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
