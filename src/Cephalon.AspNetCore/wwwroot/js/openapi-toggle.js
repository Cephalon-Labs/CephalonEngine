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

export default {
  generateOperationSlug: (operation) => {
    const method = toSlug(operation?.method);
    const path = toSlug(operation?.path);
    return `cephalon-${method}-${path}`;
  },
  onDocumentSelect: (selectedDocument) => {
    updateTitle(selectedDocument);
  },
};
