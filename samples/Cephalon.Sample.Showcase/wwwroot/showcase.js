const SHOWCASE_CONFIG = window.CEPHALON_SHOWCASE || {};
const API_ROOT = SHOWCASE_CONFIG.restApiBase || "/api/v1/showcase";
const SYSTEM_API = `${API_ROOT}/system`;
const ENDPOINTS = Object.freeze({
  systemSummary: `${SYSTEM_API}/summary`,
  systemBusiness: `${SYSTEM_API}/business`,
  systemRuntime: `${SYSTEM_API}/runtime`,
  systemDatabaseTopology: `${SYSTEM_API}/database-topology`,
  systemGovernance: `${SYSTEM_API}/governance`,
  systemTransports: `${SYSTEM_API}/transports`,
  systemActivity: `${SYSTEM_API}/activity`,
  activityStream: `${SYSTEM_API}/activity/stream`,
  reset: `${SYSTEM_API}/reset`,
  catalog: `${API_ROOT}/catalog`,
  cart: `${API_ROOT}/cart`,
  orders: `${API_ROOT}/orders`,
  inventory: `${API_ROOT}/inventory`,
  shipping: `${API_ROOT}/shipping`
});

const TRANSPORT_LABELS = Object.freeze({
  "http.rest": "REST",
  "http.graphql": "GraphQL",
  "http.jsonrpc": "JSON-RPC",
  "http.sse": "SSE",
  "http.ws": "WebSocket",
  "http.graphql-sse": "GraphQL-SSE",
  "http.graphql-ws": "GraphQL-WS"
});

const TRANSPORT_RUNNER_SCENARIOS = Object.freeze([
  {
    id: "catalog-read",
    title: "Catalog Read",
    behaviorId: "catalog.list-products",
    description: "Compare the same catalog read behavior across request-response transports that work directly in the browser.",
    detail: "No seed state required.",
    transportIds: ["http.graphql", "http.jsonrpc"]
  },
  {
    id: "cart-query",
    title: "Cart Query",
    behaviorId: "cart.get",
    description: "Validate that the active cart can be read through request, stream, and socket transports without changing the business meaning.",
    detail: "Seeds an active cart when needed.",
    transportIds: ["http.rest", "http.graphql", "http.sse", "http.ws"]
  }
]);

const DEFAULT_ADDRESS = "123 Showcase Street, Demo City";
const state = {
  cartId: loadCartId(),
  cartItems: new Map(),
  summary: null,
  business: null,
  runtime: null,
  databaseTopology: null,
  governance: null,
  transports: null,
  activity: { entries: [], totalRecorded: 0 },
  filters: loadFilterState(),
  stream: null,
  transportRunner: {
    results: {},
    busyKeys: new Set(),
    lastPreparedCartId: null,
    lastPreparedOrderId: null,
    lastRunAtUtc: null
  }
};

function init() {
  bindEvents();
  applyLinks();
  syncControlsFromState();
  renderCartComposer();
  updateNavState();
  connectActivityStream();
  refreshConsole().catch(handleConsoleRefreshError);
}

function bindEvents() {
  document.addEventListener("click", onClick);
  document.getElementById("transportSearch").addEventListener("input", (event) => {
    state.filters.search = event.target.value.trim().toLowerCase();
    syncUrlState();
    renderTransports();
  });
  document.getElementById("patternFilter").addEventListener("change", (event) => {
    state.filters.pattern = event.target.value;
    syncUrlState();
    renderTransports();
  });
  document.getElementById("transportFilter").addEventListener("change", (event) => {
    state.filters.transport = event.target.value;
    syncUrlState();
    renderTransports();
  });
  window.addEventListener("hashchange", updateNavState);
}

async function onClick(event) {
  const actionEl = event.target.closest("[data-action]");
  if (!actionEl) return;

  const action = actionEl.dataset.action;
  try {
    if (action === "refresh-all") {
      await refreshConsole();
    } else if (action === "reset-sample") {
      await resetSample();
    } else if (action === "new-cart") {
      createNewCart();
    } else if (action === "reload-cart") {
      await hydrateCartFromServer({ silent404: true });
    } else if (action === "checkout-cart") {
      await checkoutCartAndPlaceOrder();
    } else if (action === "add-product") {
      await addProductToCart(actionEl.dataset.productId);
    } else if (action === "remove-cart-line") {
      await removeCartLine(actionEl.dataset.productId);
    } else if (action === "reserve-order") {
      await reserveOrder(actionEl.dataset.orderId);
    } else if (action === "ship-order") {
      await shipOrder(actionEl.dataset.orderId);
    } else if (action === "deliver-shipment") {
      await deliverShipment(actionEl.dataset.shipmentId);
    } else if (action === "prepare-transport-runner") {
      await prepareTransportRunner();
    } else if (action === "run-all-transport-scenarios") {
      await runAllTransportScenarios();
    } else if (action === "run-transport-scenario") {
      await runTransportScenario(actionEl.dataset.scenarioId);
    } else if (action === "run-transport-probe") {
      await runTransportProbe(actionEl.dataset.scenarioId, actionEl.dataset.transportId);
    }
  } catch (error) {
    toast(error.message || "Operation failed.", "error");
  }
}

async function refreshConsole() {
  const [summary, business, runtime, transports, databaseTopology] = await Promise.all([
    requestJson(ENDPOINTS.systemSummary),
    requestJson(ENDPOINTS.systemBusiness),
    requestJson(ENDPOINTS.systemRuntime),
    requestJson(ENDPOINTS.systemTransports),
    loadDatabaseTopologyProjection()
  ]);
  let governance = state.governance;

  try {
    governance = await requestJson(ENDPOINTS.systemGovernance);
  } catch (error) {
    governance = { errorMessage: error.message || "Governance projection unavailable." };
    toast("Governance projection is temporarily unavailable.", "warning");
  }

  state.summary = summary;
  state.business = business;
  state.runtime = runtime;
  state.databaseTopology = databaseTopology;
  state.governance = governance;
  state.transports = transports;
  populateTransportFilters();
  await reconcileCartState();
  state.activity = normalizeActivityResponse(await requestJson(`${ENDPOINTS.systemActivity}?limit=40`));
  renderAll();
}

async function hydrateCartFromServer(options = {}) {
  try {
    const payload = await requestJson(`${ENDPOINTS.cart}/${encodeURIComponent(state.cartId)}`);
    const cart = unwrapPayload(payload)?.cart || unwrapPayload(payload);
    const itemsSource = Array.isArray(cart?.items) ? cart.items : Object.values(cart?.items || {});
    state.cartItems = new Map(itemsSource.map((item) => [item.productId, { ...item }]));
  } catch (error) {
    if (options.silent404 && error.status === 404) {
      state.cartItems = new Map();
      renderCartComposer();
      return;
    }
    throw error;
  }

  renderCartComposer();
}

async function resetSample() {
  const confirmed = window.confirm("Reset showcase state and reseed reference data?");
  if (!confirmed) return;

  await requestJson(ENDPOINTS.reset, { method: "POST" });
  createNewCart({ toastOnCreate: false });
  await refreshConsole();
  toast("Showcase state reset.", "success");
}

function createNewCart(options = {}) {
  state.cartId = `cart-${Math.random().toString(36).slice(2, 10)}`;
  state.cartItems = new Map();
  storeCartId(state.cartId);
  syncUrlState();
  renderCartComposer();
  if (options.toastOnCreate !== false) {
    toast(`Switched to ${state.cartId}.`, "success");
  }
}

async function addProductToCart(productId, options = {}) {
  const product = state.business?.products?.find((item) => item.id === productId);
  if (!product) throw new Error("Product not found.");

  await requestJson(`${ENDPOINTS.cart}/${encodeURIComponent(state.cartId)}/items`, {
    method: "POST",
    body: {
      cartId: state.cartId,
      customerId: "showcase-web-ui",
      productId: product.id,
      productName: product.name,
      quantity: 1,
      priceInCents: product.priceInCents
    }
  });

  const existing = state.cartItems.get(product.id);
  state.cartItems.set(product.id, {
    productId: product.id,
    productName: product.name,
    quantity: (existing?.quantity || 0) + 1,
    priceInCents: product.priceInCents
  });

  renderCartComposer();
  await refreshWorkloadData();
  if (options.toastOnSuccess !== false) {
    toast(`${product.name} added to ${state.cartId}.`, "success");
  }
}

async function removeCartLine(productId) {
  await requestJson(`${ENDPOINTS.cart}/${encodeURIComponent(state.cartId)}/items/${encodeURIComponent(productId)}`, {
    method: "DELETE"
  });

  state.cartItems.delete(productId);
  renderCartComposer();
  await refreshWorkloadData();
  toast("Cart item removed.", "warning");
}

async function checkoutCartAndPlaceOrder(options = {}) {
  const lines = [...state.cartItems.values()];
  if (!lines.length) throw new Error("Add items before checking out.");

  const checkoutResult = unwrapPayload(await requestJson(`${ENDPOINTS.cart}/${encodeURIComponent(state.cartId)}/checkout`, {
    method: "POST",
    body: { cartId: state.cartId, shippingAddress: DEFAULT_ADDRESS }
  }));
  const checkedOutOrderId = checkoutResult?.orderId;

  const orderPayload = {
    orderId: checkedOutOrderId,
    customerId: "showcase-web-ui",
    shippingAddress: DEFAULT_ADDRESS,
    items: lines.map((item) => ({
      productId: item.productId,
      productName: item.productName,
      quantity: item.quantity,
      unitPriceInCents: item.priceInCents
    }))
  };

  const orderResult = await requestJson(ENDPOINTS.orders, {
    method: "POST",
    body: orderPayload
  });

  const orderId = unwrapPayload(orderResult)?.orderId || orderResult.orderId || checkedOutOrderId || "new order";
  if (options.createReplacementCart !== false) {
    createNewCart({ toastOnCreate: false });
  }

  if (options.refreshConsole !== false) {
    await refreshConsole();
  }

  if (options.toastOnSuccess !== false) {
    toast(`Checkout complete. Placed ${orderId}.`, "success");
  }

  return { orderId, checkedOutOrderId, itemCount: lines.length };
}

async function reserveOrder(orderId) {
  const order = unwrapPayload(await requestJson(`${ENDPOINTS.orders}/${encodeURIComponent(orderId)}`));
  await requestJson(`${ENDPOINTS.inventory}/reserve`, {
    method: "POST",
    body: {
      orderId,
      items: order.items.map((item) => ({ productId: item.productId, quantity: item.quantity }))
    }
  });
  await refreshWorkloadData();
  toast(`Reserved stock for ${orderId}.`, "success");
}

async function shipOrder(orderId) {
  const order = unwrapPayload(await requestJson(`${ENDPOINTS.orders}/${encodeURIComponent(orderId)}`));
  await requestJson(ENDPOINTS.shipping, {
    method: "POST",
    body: {
      orderId,
      destinationAddress: order.shippingAddress,
      items: order.items.map((item) => ({
        productId: item.productId,
        productName: item.productName,
        quantity: item.quantity
      }))
    }
  });
  await refreshWorkloadData();
  toast(`Created shipment for ${orderId}.`, "success");
}

async function deliverShipment(shipmentId) {
  await requestJson(`${ENDPOINTS.shipping}/${encodeURIComponent(shipmentId)}/deliver`, {
    method: "PUT",
    body: { shipmentId, recipientName: "Showcase Operator" }
  });
  await refreshWorkloadData();
  toast(`Delivered ${shipmentId}.`, "success");
}

async function refreshWorkloadData() {
  const [summary, business, databaseTopology] = await Promise.all([
    requestJson(ENDPOINTS.systemSummary),
    requestJson(ENDPOINTS.systemBusiness),
    loadDatabaseTopologyProjection()
  ]);
  state.summary = summary;
  state.business = business;
  state.databaseTopology = databaseTopology;
  await reconcileCartState();
  state.activity = normalizeActivityResponse(await requestJson(`${ENDPOINTS.systemActivity}?limit=40`));
  renderAll();
}

async function reconcileCartState() {
  const openCarts = (state.business?.carts || []).filter((cart) => !cart.isCheckedOut);
  const matchingCart = openCarts.find((cart) => cart.cartId === state.cartId);
  if (matchingCart) {
    await hydrateCartFromServer();
    return;
  }

  if (!state.cartItems.size && openCarts.length > 0) {
    state.cartId = openCarts[0].cartId;
    storeCartId(state.cartId);
    syncUrlState();
    await hydrateCartFromServer();
    return;
  }

  state.cartItems = new Map();
  renderCartComposer();
}

function connectActivityStream() {
  if (state.stream) state.stream.close();
  const source = new EventSource(ENDPOINTS.activityStream);
  state.stream = source;

  source.addEventListener("ready", () => updateStreamStatus(true));
  source.addEventListener("activity", (event) => {
    const entry = normalizeActivityEntry(JSON.parse(event.data));
    state.activity.entries = [entry, ...(state.activity.entries || [])].slice(0, 80);
    state.activity.totalRecorded = Math.max(state.activity.totalRecorded || 0, entry.sequence || 0);
    renderActivity();
  });
  source.onerror = () => updateStreamStatus(false);
}

function renderAll() {
  renderOverview();
  renderDatabaseTopology();
  renderWorkloads();
  renderRuntime();
  renderGovernance();
  renderTransports();
  renderActivity();
}

function renderOverview() {
  if (!state.summary) return;
  const { runtime, business, dependencies, suggestedJourneys, documentation } = state.summary;
  setPill("runtimeStatusPill", `Runtime: ${runtime.status}`, tone(runtime.status));
  setPill("readinessPill", `Readiness: ${runtime.readiness}`, tone(runtime.readiness));
  setPill("livenessPill", `Liveness: ${runtime.liveness}`, tone(runtime.liveness));
  document.getElementById("summaryTimestamp").textContent = `Updated ${formatDate(business.generatedAtUtc)}`;

  document.getElementById("overviewKpis").innerHTML = [
    kpiCard("Modules", runtime.moduleCount, runtime.blueprintDisplayName),
    kpiCard("Capabilities", runtime.capabilityCount, `${runtime.behaviorCount} behaviors`),
    kpiCard("Orders", business.orders, `${business.pendingOrders} pending`),
    kpiCard("Revenue", money(business.revenueInCents), `${business.deliveredOrders} delivered`),
    kpiCard("Shipments", business.shipments, `${business.deliveredShipments} delivered`),
    kpiCard("Activity", state.activity.totalRecorded || 0, "server-backed feed")
  ].join("");

  document.getElementById("runtimeSummaryList").innerHTML = [
    metricRow("Environment", runtime.environment),
    metricRow("Blueprint", runtime.blueprintDisplayName),
    metricRow("Database roles", `${runtime.writeProvider} / ${runtime.readProvider} / ${runtime.historyProvider}`),
    metricRow("Transports", runtime.transportCount),
    metricRow("Technologies", runtime.technologyCount),
    metricRow("Diagnostics", runtime.diagnosticsConventionCount),
    metricRow("Restart count", runtime.restartCount),
    metricRow("Started", runtime.startedAtUtc ? formatDate(runtime.startedAtUtc) : "not started")
  ].join("");

  document.getElementById("dependencyList").innerHTML = dependencies.length
    ? dependencies.map((item) => `
      <div class="dependency-item">
        <header><strong>${escapeHtml(item.displayName)}</strong><span class="status-badge ${tone(item.state)}">${escapeHtml(item.state)}</span></header>
        <small>${escapeHtml(item.description)}</small>
      </div>`).join("")
    : `<div class="empty-state">No dependency health entries published.</div>`;

  document.getElementById("journeyList").innerHTML = suggestedJourneys.map((item) => `<li>${escapeHtml(item)}</li>`).join("");
  document.getElementById("documentationLinks").innerHTML = [
    linkButton("Scalar", documentation.scalarPath),
    linkButton("OpenAPI JSON", documentation.openApiJsonPath),
    linkButton("Runtime Snapshot", documentation.runtimeSnapshotPath),
    linkButton("Runtime Story", documentation.runtimeStoryPath),
    linkButton("Diagnostics", documentation.diagnosticsPath),
    linkButton("Audit History", documentation.auditHistoryPath),
    linkButton("Database Topology", documentation.databaseTopologyPath)
  ].join("");
}

function renderDatabaseTopology() {
  renderDatabaseTopologyLinks();
  if (!state.databaseTopology) return;

  if (state.databaseTopology.errorMessage) {
    const message = escapeHtml(state.databaseTopology.errorMessage);
    document.getElementById("databaseTopologyTimestamp").textContent = "Database topology projection unavailable";
    document.getElementById("databaseTopologyKpis").innerHTML = `<div class="empty-state">${message}</div>`;
    document.getElementById("databaseTopologyInsights").innerHTML = `<div class="empty-state">${message}</div>`;
    document.getElementById("databaseRoleTable").innerHTML = `<tr><td colspan="5" class="empty-state">${message}</td></tr>`;
    document.getElementById("databaseMigrationTable").innerHTML = `<tr><td colspan="5" class="empty-state">${message}</td></tr>`;
    document.getElementById("readModelSyncSummary").innerHTML = `<div class="empty-state">${message}</div>`;
    document.getElementById("readModelSyncTimeline").innerHTML = `<div class="empty-state">${message}</div>`;
    document.getElementById("readModelStoreTable").innerHTML = `<tr><td colspan="4" class="empty-state">${message}</td></tr>`;
    document.getElementById("readModelScopeList").innerHTML = `<div class="empty-state">${message}</div>`;
    return;
  }

  const { summary, insights, roles, migrations, readModelSync } = state.databaseTopology;
  const totalDeltaMagnitude = getReadModelDeltaMagnitude(readModelSync);
  const attentionCount = (insights || []).filter((insight) => tone(insight?.tone) !== "status-success").length;
  const recommendedMigrationTargets = (migrations || []).filter((migration) => hasRecommendedMigrationCommands(migration.commands)).length;
  document.getElementById("databaseTopologyTimestamp").textContent = `Updated ${formatDate(summary.generatedAtUtc)}`;
  renderDatabaseTopologyInsights(insights);

  document.getElementById("databaseTopologyKpis").innerHTML = [
    kpiCard("Roles", summary.roleCount, `${summary.healthyRoleCount} healthy`),
    kpiCard("Migrations", summary.migrationTargetCount, `${summary.succeededMigrationTargetCount} succeeded / ${recommendedMigrationTargets} production-guided`),
    kpiCard("Sync", readModelSync.enabled ? (readModelSync.isLagging ? "Lagging" : "Aligned") : "Disabled", readModelSync.enabled ? "read-model loop active" : "projection loop inactive"),
    kpiCard("Projection Jobs", readModelSync.jobs.totalJobs, `${readModelSync.jobs.pendingJobs} pending / ${readModelSync.jobs.failedJobs} failed`),
    kpiCard("Store Delta", totalDeltaMagnitude, totalDeltaMagnitude === 0 ? "write and read aligned" : "write minus read drift"),
    kpiCard("Attention", attentionCount, attentionCount === 0 ? "topology aligned" : "operator insights to review")
  ].join("");

  document.getElementById("databaseRoleTable").innerHTML = roles.length
    ? roles.map((role) => `
      <tr>
        <td>
          <strong>${escapeHtml(role.id)}</strong>
          <div class="mono">${escapeHtml(role.requestedRoleId)} -> ${escapeHtml(role.resolvedRoleId)}</div>
          <div class="meta-row">
            <span class="token">${escapeHtml(role.resolutionMode)}</span>
            ${role.connectionMode ? `<span class="token">${escapeHtml(role.connectionMode)}</span>` : ""}
            ${role.schema ? `<span class="token">schema ${escapeHtml(role.schema)}</span>` : ""}
          </div>
        </td>
        <td>
          <strong>${escapeHtml(role.provider)}</strong>
        </td>
        <td>
          <div class="status-stack">
            <span class="status-badge ${tone(role.healthState || "")}">${escapeHtml(role.healthState || "Unknown")}</span>
            <span class="status-badge ${tone(role.migrationState || "")}">${escapeHtml(role.migrationState || "Unknown")}</span>
          </div>
        </td>
        <td>
          ${role.consumers.length
            ? `<div class="meta-row">${role.consumers.map((consumer) => `<span class="token">${escapeHtml(consumer)}</span>`).join("")}</div>`
            : `<div class="empty-inline">No consumers</div>`}
        </td>
        <td>${renderMetadataSections([
          ["Declared", role.metadataPreview],
          ["Runtime", role.runtimeMetadataPreview]
        ])}</td>
      </tr>`).join("")
    : `<tr><td colspan="5" class="empty-state">No database roles published.</td></tr>`;

  document.getElementById("databaseMigrationTable").innerHTML = migrations.length
    ? migrations.map((migration) => `
      <tr>
        <td>
          <strong>${escapeHtml(migration.id)}</strong>
          <div class="mono">${escapeHtml(migration.requestedRoleId)} -> ${escapeHtml(migration.resolvedRoleId)}</div>
          ${migration.dbContextType ? `<div class="meta-row"><span class="token">${escapeHtml(shortTypeName(migration.dbContextType))}</span></div>` : ""}
        </td>
        <td>
          <div class="status-stack">
            <span class="status-badge ${tone(migration.status)}">${escapeHtml(migration.status)}</span>
            <span class="status-badge ${migration.applyOnStartup ? "status-success" : ""}">${migration.applyOnStartup ? "apply on startup" : "manual"}</span>
          </div>
        </td>
        <td>
          <strong>${escapeHtml(migration.executionMode)}</strong>
          ${migration.provider ? `<div class="mono">${escapeHtml(migration.provider)}</div>` : ""}
        </td>
        <td>${renderMigrationCommandGuidance(migration.commands)}</td>
        <td>${renderMetadataSections([
          ["Metadata", migration.metadataPreview]
        ], "No migration metadata preview available.")}</td>
      </tr>`).join("")
    : `<tr><td colspan="5" class="empty-state">No migration targets published.</td></tr>`;

  document.getElementById("readModelSyncSummary").innerHTML = [
    statCard("Sync loop", readModelSync.enabled ? "Enabled" : "Disabled", readModelSync.isLagging ? "lagging" : "stores aligned"),
    statCard("Pending jobs", readModelSync.jobs.pendingJobs, readModelSync.jobs.failedJobs ? `${readModelSync.jobs.failedJobs} failed` : "no failed jobs"),
    statCard("Products delta", formatSignedNumber(readModelSync.productDelta), "write minus read"),
    statCard("Inventory delta", formatSignedNumber(readModelSync.inventoryDelta), "write minus read"),
    statCard("Orders delta", formatSignedNumber(readModelSync.orderDelta), "write minus read"),
    statCard("Shipments delta", formatSignedNumber(readModelSync.shipmentDelta), "write minus read")
  ].join("");

  document.getElementById("readModelSyncTimeline").innerHTML = [
    metricRow("Distinct job scopes", readModelSync.jobs.distinctScopes),
    metricRow("Completed jobs", readModelSync.jobs.completedJobs),
    metricRow("Next available job", readModelSync.jobs.nextAvailableAtUtc ? formatDate(readModelSync.jobs.nextAvailableAtUtc) : "none pending"),
    metricRow("Last completed job", readModelSync.jobs.lastCompletedAtUtc ? formatDate(readModelSync.jobs.lastCompletedAtUtc) : "not yet")
  ].join("");

  document.getElementById("readModelStoreTable").innerHTML = [
    buildReadModelStoreRow("Products", readModelSync.writeStore.products, readModelSync.readStore.products, readModelSync.productDelta),
    buildReadModelStoreRow("Inventory", readModelSync.writeStore.inventory, readModelSync.readStore.inventory, readModelSync.inventoryDelta),
    buildReadModelStoreRow("Orders", readModelSync.writeStore.orders, readModelSync.readStore.orders, readModelSync.orderDelta),
    buildReadModelStoreRow("Shipments", readModelSync.writeStore.shipments, readModelSync.readStore.shipments, readModelSync.shipmentDelta)
  ].join("");

  document.getElementById("readModelScopeList").innerHTML = readModelSync.scopes.length
    ? readModelSync.scopes.map((scope) => renderProjectionScopeCard(scope)).join("")
    : `<div class="empty-state">No projection scopes published.</div>`;
}

function renderDatabaseTopologyInsights(insights) {
  const items = Array.isArray(insights) ? insights : [];
  document.getElementById("databaseTopologyInsights").innerHTML = items.length
    ? items.map((insight) => `
      <article class="insight-card ${tone(insight.tone)}">
        <header>
          <strong>${escapeHtml(insight.title)}</strong>
          <span class="status-badge ${tone(insight.tone)}">${escapeHtml(insight.tone)}</span>
        </header>
        <p>${escapeHtml(insight.detail)}</p>
        <div class="insight-actions">
          ${insight.actionPath ? linkButton(insight.actionLabel || "Open", insight.actionPath) : ""}
        </div>
      </article>`).join("")
    : `<div class="empty-state">No operator insights published.</div>`;
}

function renderWorkloads() {
  if (!state.business) return;
  document.getElementById("productTable").innerHTML = state.business.products.map((product) => `
    <tr>
      <td><strong>${escapeHtml(product.name)}</strong><div class="mono">${escapeHtml(product.id)}</div></td>
      <td>${escapeHtml(product.category)}</td>
      <td class="price">${money(product.priceInCents)}</td>
      <td><button class="btn btn-sm btn-outline" data-action="add-product" data-product-id="${escapeHtml(product.id)}">Add</button></td>
    </tr>`).join("");

  document.getElementById("orderTable").innerHTML = state.business.orders.length
    ? state.business.orders.map((order) => `
      <tr>
        <td><strong class="mono">${shortId(order.orderId)}</strong><div>${escapeHtml(order.customerId)}</div></td>
        <td><span class="status-badge ${tone(order.status)}">${escapeHtml(order.status)}</span></td>
        <td class="price">${money(order.totalInCents)}</td>
        <td>${orderActions(order)}</td>
      </tr>`).join("")
    : `<tr><td colspan="4" class="empty-state">No orders yet.</td></tr>`;

  document.getElementById("inventoryTable").innerHTML = state.business.inventory.map((item) => `
    <tr>
      <td><strong>${escapeHtml(item.productName)}</strong></td>
      <td>${item.quantityAvailable}/${item.quantityOnHand}</td>
      <td>${item.quantityReserved}</td>
    </tr>`).join("");

  document.getElementById("shipmentTable").innerHTML = state.business.shipments.length
    ? state.business.shipments.map((shipment) => `
      <tr>
        <td><strong class="mono">${shortId(shipment.shipmentId)}</strong></td>
        <td class="mono">${shortId(shipment.orderId)}</td>
        <td><span class="status-badge ${tone(shipment.status)}">${escapeHtml(shipment.status)}</span></td>
        <td>${escapeHtml(shipment.carrier)}</td>
        <td>${shipment.status === "Delivered" ? "" : `<button class="btn btn-sm btn-outline" data-action="deliver-shipment" data-shipment-id="${escapeHtml(shipment.shipmentId)}">Deliver</button>`}</td>
      </tr>`).join("")
    : `<tr><td colspan="5" class="empty-state">No shipments yet.</td></tr>`;

  renderCartComposer();
}

function renderCartComposer() {
  const items = [...state.cartItems.values()];
  const total = items.reduce((sum, item) => sum + item.quantity * item.priceInCents, 0);
  const quantity = items.reduce((sum, item) => sum + item.quantity, 0);
  document.getElementById("cartIdDisplay").textContent = state.cartId;
  document.getElementById("cartItemCount").textContent = quantity;
  document.getElementById("cartTotal").textContent = money(total);
  document.getElementById("checkoutButton").disabled = items.length === 0;
  document.getElementById("cartItems").innerHTML = items.length
    ? items.map((item) => `
      <div class="cart-line">
        <div>
          <strong>${escapeHtml(item.productName)}</strong>
          <div class="mono">${escapeHtml(item.productId)}</div>
        </div>
        <div>
          <span>${item.quantity} x ${money(item.priceInCents)}</span>
          <button class="btn btn-sm btn-ghost" data-action="remove-cart-line" data-product-id="${escapeHtml(item.productId)}">Remove</button>
        </div>
      </div>`).join("")
    : `<div class="empty-state">Cart is empty.</div>`;
}

function renderRuntime() {
  if (!state.runtime) return;
  document.getElementById("facetGrid").innerHTML = state.runtime.facets.map((facet) => `
    <div class="stat-card">
      <span>${escapeHtml(facet.key)}</span>
      <strong>${facet.count}</strong>
      <small>${escapeHtml(facet.description)}</small>
    </div>`).join("");

  document.getElementById("moduleTable").innerHTML = state.runtime.modules.map((module) => `
    <tr>
      <td><strong>${escapeHtml(module.displayName)}</strong><div class="mono">${escapeHtml(module.id)}</div></td>
      <td>${escapeHtml(module.version)}</td>
      <td>${module.isTrusted ? `<span class="status-badge status-success">trusted</span>` : `<span class="status-badge status-error">untrusted</span>`}</td>
      <td>${module.dependencyCount}</td>
    </tr>`).join("");

  document.getElementById("capabilityList").innerHTML = state.runtime.capabilities.map((capability) => `
    <div class="capability-item">
      <strong>${escapeHtml(capability.displayName)}</strong>
      <small>${escapeHtml(capability.sourceModuleId)}</small>
      <div>${escapeHtml(capability.description)}</div>
    </div>`).join("");

  document.getElementById("patternTokens").innerHTML = state.runtime.patterns.map((item) => `<span class="token">${escapeHtml(item.displayName)}</span>`).join("");
  document.getElementById("transportTokens").innerHTML = state.runtime.transports.map((item) => `<span class="token">${escapeHtml(item.displayName)}</span>`).join("");

  const audits = state.summary?.recentAuditEntries || [];
  document.getElementById("auditList").innerHTML = audits.length
    ? audits.map((entry) => `
      <div class="audit-item">
        <header><strong>${escapeHtml(entry.action)}</strong><span class="status-badge ${tone(entry.outcome)}">${escapeHtml(entry.outcome)}</span></header>
        <div>${escapeHtml(entry.summary)}</div>
        <div class="meta-row"><span class="mono">${escapeHtml(entry.category)}</span><span>${formatDate(entry.occurredAtUtc)}</span></div>
      </div>`).join("")
    : `<div class="empty-state">No audit entries available yet.</div>`;
}

function renderGovernance() {
  if (!state.governance) return;
  if (state.governance.errorMessage) {
    document.getElementById("governanceSummary").innerHTML = `<div class="empty-state">${escapeHtml(state.governance.errorMessage)}</div>`;
    document.getElementById("governanceLinks").innerHTML = "";
    document.getElementById("packagePolicyList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("trustSummaryList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("capabilityDecisionList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("loadedPackageList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("authorizationPolicyList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("technologySurfaceList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("runtimeStoryList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    document.getElementById("timelineList").innerHTML = `<div class="empty-state">Governance projection unavailable.</div>`;
    return;
  }

  const { summary, packagePolicy, trust, capabilityDecisions, packages, authorizationPolicies, technologySurfaces, runtimeStory, recentTimeline } = state.governance;
  const engine = SHOWCASE_CONFIG.engine || {};

  document.getElementById("governanceSummary").innerHTML = [
    statCard("Loaded packages", summary.loadedPackageCount, summary.loadedPackageCount ? `${summary.trustedPackageCount} trusted` : "assembly-first sample"),
    statCard("Strict package rules", summary.packagePolicyRequirementCount, summary.allowAssemblyPathPackages ? "assembly path allowed" : "manifest-first"),
    statCard("Capability access", summary.capabilityAllowedCount, `${summary.capabilityBlockedCount} blocked now`),
    statCard("Authorization", summary.authorizationPolicyCount, "published policies"),
    statCard("Tech surfaces", summary.technologySurfaceCount, `${summary.technologyEntryCount} entries`),
    statCard("Runtime story", summary.timelineEventCount, "timeline events")
  ].join("");

  document.getElementById("governanceLinks").innerHTML = [
    linkButton("Packages", engine.packages || "/engine/packages"),
    linkButton("Package Policy", engine.packagePolicy || "/engine/package-policy"),
    linkButton("Trust Policy", engine.trustPolicy || "/engine/trust-policy"),
    linkButton("Authorization", engine.authorizationPolicies || "/engine/authorization-policies"),
    linkButton("Technology", engine.technologySurfaces || "/engine/technology-surfaces"),
    linkButton("Runtime Story", engine.runtimeStory || "/engine/runtime-story")
  ].join("");

  document.getElementById("packagePolicyList").innerHTML = packagePolicy.length
    ? packagePolicy.map((rule) => `
      <div class="capability-item policy-rule">
        <header>
          <strong>${escapeHtml(rule.displayName)}</strong>
          <span class="status-badge ${rule.enabled ? "status-warning" : ""}">${rule.enabled ? "enabled" : "disabled"}</span>
        </header>
        <small class="mono">${escapeHtml(rule.key)}</small>
        <div>${escapeHtml(rule.description)}</div>
      </div>`).join("")
    : `<div class="empty-state">No package policy rules found.</div>`;

  document.getElementById("trustSummaryList").innerHTML = [
    metricRow("Require trusted packages", trust.requireTrustedPackages ? "yes" : "no"),
    metricRow("Default capability access", trust.defaultCapabilityAccess),
    metricRow("Trusted assemblies", trust.trustedAssemblyCount),
    metricRow("Trusted packages", trust.trustedPackageAllowListCount),
    metricRow("Trusted publishers", trust.trustedPublisherCount),
    metricRow("Trusted signers", trust.trustedSignerCount),
    metricRow("Trusted public keys", trust.trustedPublicKeyCount),
    metricRow("Trusted certificates", trust.trustedCertificateCount),
    metricRow("Trusted certificate authorities", trust.trustedCertificateAuthorityCount),
    metricRow("Allowed checksum rules", trust.allowedChecksumRuleCount),
    metricRow("Capability overrides", trust.capabilityOverrideCount)
  ].join("");

  document.getElementById("capabilityDecisionList").innerHTML = capabilityDecisions.map((item) => `
    <div class="stat-card">
      <span>${escapeHtml(item.key)}</span>
      <strong>${item.count}</strong>
      <small>${escapeHtml(item.description)}</small>
    </div>`).join("");

  document.getElementById("loadedPackageList").innerHTML = packages.length
    ? packages.map((pkg) => `
      <article class="package-item">
        <header>
          <strong>${escapeHtml(pkg.packageId)}</strong>
          <div class="meta-row">
            <span class="status-badge ${pkg.isTrusted ? "status-success" : "status-error"}">${pkg.isTrusted ? "trusted" : "untrusted"}</span>
            <span class="status-badge ${pkg.isSignatureVerified ? "status-success" : ""}">${pkg.isSignatureVerified ? "verified" : "not verified"}</span>
          </div>
        </header>
        <div class="meta-row">
          <span class="token">${escapeHtml(pkg.kind)}</span>
          <span class="token">${escapeHtml(pkg.assemblyName)}</span>
          ${pkg.version ? `<span class="token">v${escapeHtml(pkg.version)}</span>` : ""}
          ${pkg.publisherId ? `<span class="token">${escapeHtml(pkg.publisherId)}</span>` : ""}
          ${pkg.modules.map((moduleId) => `<span class="token">${escapeHtml(moduleId)}</span>`).join("")}
        </div>
        <small>${escapeHtml(pkg.trustReason)}</small>
      </article>`).join("")
    : `<div class="empty-state">This sample is currently running from in-repo assemblies, so there are no independently loaded packages yet.</div>`;

  document.getElementById("authorizationPolicyList").innerHTML = authorizationPolicies.length
    ? authorizationPolicies.map((policy) => `
      <article class="capability-item authorization-policy-item">
        <header>
          <strong>${escapeHtml(policy.displayName)}</strong>
          <span class="status-badge">${policy.modes.length} modes</span>
        </header>
        <small class="mono">${escapeHtml(policy.id)}</small>
        <div>${escapeHtml(policy.description)}</div>
        <div class="meta-row">
          ${policy.modes.map((mode) => `<span class="token">${escapeHtml(mode)}</span>`).join("")}
          ${policy.tags.map((tag) => `<span class="token">${escapeHtml(tag)}</span>`).join("")}
          ${renderMetadataPreview(policy.metadataPreview)}
        </div>
      </article>`).join("")
    : `<div class="empty-state">No authorization policies are currently published.</div>`;

  document.getElementById("technologySurfaceList").innerHTML = technologySurfaces.length
    ? technologySurfaces.map((surface) => `
      <article class="surface-card">
        <header>
          <div>
            <strong>${escapeHtml(surface.displayName)}</strong>
            <div class="mono">${escapeHtml(surface.technologyId)} / ${escapeHtml(surface.surfaceId)}</div>
          </div>
          <span class="status-badge">${surface.entryCount} entries</span>
        </header>
        <p>${escapeHtml(surface.description)}</p>
        <div class="surface-entry-list">
          ${surface.entries.map((entry) => `
            <div class="capability-item surface-entry">
              <strong>${escapeHtml(entry.displayName)}</strong>
              <small class="mono">${escapeHtml(entry.id)}</small>
              <div>${escapeHtml(entry.description)}</div>
              <div class="meta-row">${renderMetadataPreview(entry.metadataPreview)}</div>
            </div>`).join("")}
        </div>
      </article>`).join("")
    : `<div class="empty-state">No technology surfaces are active.</div>`;

  document.getElementById("runtimeStoryList").innerHTML = [
    metricRow("Generated", formatDate(runtimeStory.generatedAtUtc)),
    metricRow("Status", runtimeStory.status),
    metricRow("Started", runtimeStory.startedAtUtc ? formatDate(runtimeStory.startedAtUtc) : "not started"),
    metricRow("Modules", `${runtimeStory.startedModuleCount}/${runtimeStory.moduleCount} started`),
    metricRow("Execution graphs", `${runtimeStory.activeExecutionGraphCount}/${runtimeStory.executionGraphCount} active`),
    metricRow("Hosted executions", `${runtimeStory.activeHostedExecutionCount}/${runtimeStory.hostedExecutionCount} active`),
    metricRow("Loaded packages", runtimeStory.loadedPackageCount),
    metricRow("Timeline events", runtimeStory.timelineEventCount)
  ].join("");

  document.getElementById("timelineList").innerHTML = recentTimeline.length
    ? recentTimeline.map((entry) => `
      <article class="timeline-item">
        <header>
          <strong>${escapeHtml(entry.phase)}</strong>
          <span class="status-badge ${tone(entry.outcome)}">${escapeHtml(entry.outcome)}</span>
        </header>
        <div>${escapeHtml(entry.message)}</div>
        <div class="meta-row">
          <span class="token">${escapeHtml(entry.scope)}</span>
          ${entry.subjectId ? `<span class="token mono">${escapeHtml(entry.subjectId)}</span>` : ""}
          <span>${formatDate(entry.occurredAtUtc)}</span>
        </div>
      </article>`).join("")
    : `<div class="empty-state">No lifecycle timeline entries available.</div>`;
}

function renderTransports() {
  if (!state.transports) return;
  renderTransportRunner();
  const behaviorById = new Map(state.transports.behaviors.map((behavior) => [behavior.behaviorId, behavior]));
  const filtered = state.transports.behaviors.filter((behavior) => {
    const haystack = `${behavior.behaviorId} ${behavior.pattern} ${behavior.routes.map((route) => route.route).join(" ")}`.toLowerCase();
    const matchesSearch = !state.filters.search || haystack.includes(state.filters.search);
    const matchesPattern = !state.filters.pattern || behavior.pattern === state.filters.pattern;
    const matchesTransport = !state.filters.transport || behavior.transportIds.includes(state.filters.transport);
    return matchesSearch && matchesPattern && matchesTransport;
  });
  const filteredBehaviorIds = new Set(filtered.map((behavior) => behavior.behaviorId));
  const filteredOperations = state.transports.restOperations.filter((operation) => {
    const behavior = operation.behaviorId ? behaviorById.get(operation.behaviorId) : null;
    const haystack = `${operation.method} ${operation.route} ${operation.displayName || ""} ${operation.moduleId || ""} ${operation.behaviorId || ""}`.toLowerCase();
    const matchesSearch = !state.filters.search || haystack.includes(state.filters.search);
    const matchesPattern = !state.filters.pattern || (behavior && behavior.pattern === state.filters.pattern);
    const matchesTransport = !state.filters.transport || state.filters.transport === "http.rest";
    const matchesBehaviorScope = !operation.behaviorId || filteredBehaviorIds.has(operation.behaviorId);
    return matchesSearch && matchesPattern && matchesTransport && matchesBehaviorScope;
  });

  document.getElementById("transportSummary").innerHTML = [
    statCard("Behavior topology", state.transports.summary.behaviorCount, "owned behaviors"),
    statCard("Filtered behaviors", filtered.length, "current view"),
    statCard("REST operations", filteredOperations.length, "public routes"),
    statCard("Active transports", state.transports.summary.transportCount, "runtime surface")
  ].join("");

  document.getElementById("behaviorMatrix").innerHTML = filtered.length
    ? filtered.map((behavior) => `
      <article class="behavior-card">
        <header>
          <div>
            <strong>${escapeHtml(behavior.behaviorId)}</strong>
            <div>${escapeHtml(behavior.pattern)}</div>
          </div>
          <span class="status-badge">${behavior.transportIds.length} transports</span>
        </header>
        <div class="meta-row">${behavior.transportIds.map((item) => `<span class="token">${escapeHtml(item)}</span>`).join("")}</div>
        <div class="behavior-routes">
          ${behavior.routes.map((route) => `<span class="token ${route.canonical ? "tag-success" : ""}"><strong>${escapeHtml(route.method)}</strong> ${escapeHtml(route.route)}</span>`).join("")}
        </div>
      </article>`).join("")
    : `<div class="empty-state">No behaviors match the current filter.</div>`;

  document.getElementById("restOperationList").innerHTML = filteredOperations.length
    ? filteredOperations.map((operation) => `
      <article class="rest-operation">
        <header><strong>${escapeHtml(operation.method)}</strong><span class="mono">${escapeHtml(operation.route)}</span></header>
        <div class="meta-row">
          ${operation.moduleId ? `<span class="token">${escapeHtml(operation.moduleId)}</span>` : ""}
          ${operation.behaviorId ? `<span class="token">${escapeHtml(operation.behaviorId)}</span>` : ""}
        </div>
      </article>`).join("")
    : `<div class="empty-state">No REST operations match the current filter.</div>`;
}

function renderTransportRunner() {
  const scenarios = getTransportRunnerScenarios();
  const activeProbeCount = scenarios.reduce((sum, scenario) => sum + scenario.probes.filter((probe) => probe.route).length, 0);

  document.getElementById("transportRunnerContext").innerHTML = [
    statCard("Runner cart", state.transportRunner.lastPreparedCartId || state.cartId, "current transport seed"),
    statCard("Catalog rows", state.business?.products?.length || 0, "read-only comparison seed"),
    statCard("Browser probes", activeProbeCount, `${scenarios.length} behavior scenarios`),
    statCard("Last validation", state.transportRunner.lastRunAtUtc ? formatDate(state.transportRunner.lastRunAtUtc) : "not run yet", "latest probe timestamp")
  ].join("");

  document.getElementById("transportRunnerGrid").innerHTML = scenarios.map((scenario) => {
    const availableCount = scenario.probes.filter((probe) => probe.route).length;
    const scenarioBusy = scenario.probes.some((probe) => probe.isBusy);

    return `
      <article class="runner-scenario">
        <header>
          <div>
            <strong>${escapeHtml(scenario.title)}</strong>
            <div class="mono">${escapeHtml(scenario.behaviorId)}</div>
          </div>
          <span class="status-badge ${availableCount ? "" : "status-error"}">${availableCount}/${scenario.probes.length} routes</span>
        </header>
        <p>${escapeHtml(scenario.description)}</p>
        <div class="meta-row">
          ${scenario.transportIds.map((transportId) => `<span class="token">${escapeHtml(TRANSPORT_LABELS[transportId] || transportId)}</span>`).join("")}
          <span class="token">${escapeHtml(scenario.detail)}</span>
        </div>
        <div class="runner-actions">
          <button
            class="btn btn-sm btn-outline"
            data-action="run-transport-scenario"
            data-scenario-id="${escapeHtml(scenario.id)}"
            ${availableCount === 0 || scenarioBusy ? "disabled" : ""}>
            ${scenarioBusy ? "Running..." : "Run scenario"}
          </button>
          <small>${escapeHtml(availableCount ? "Executes each published browser-friendly route for this behavior." : "This runtime is not publishing the expected routes for this scenario.")}</small>
        </div>
        <div class="runner-probe-grid">
          ${scenario.probes.map((probe) => renderTransportProbeCard(scenario, probe)).join("")}
        </div>
      </article>`;
  }).join("");
}

function renderTransportProbeCard(scenario, probe) {
  const statusLabel = probe.isBusy
    ? "running"
    : probe.result
      ? (probe.result.succeeded ? "passed" : "failed")
      : (probe.route ? "ready" : "unavailable");
  const statusTone = probe.isBusy
    ? "status-warning"
    : probe.result
      ? (probe.result.succeeded ? "status-success" : "status-error")
      : "";
  const routeLabel = probe.route
    ? `${probe.route.method} ${probe.route.route}`
    : "Route not published in this runtime.";
  const summary = probe.result
    ? `${formatDuration(probe.result.durationMs)} · ${probe.result.summary}`
    : scenario.detail;
  const preview = probe.result?.preview || `Select Run to execute ${TRANSPORT_LABELS[probe.transportId] || probe.transportId}.`;

  return `
    <article class="runner-probe">
      <header>
        <strong>${escapeHtml(probe.label)}</strong>
        <span class="status-badge ${statusTone}">${escapeHtml(statusLabel)}</span>
      </header>
      <div class="mono runner-route">${escapeHtml(routeLabel)}</div>
      <div class="runner-actions">
        <button
          class="btn btn-sm btn-ghost"
          data-action="run-transport-probe"
          data-scenario-id="${escapeHtml(scenario.id)}"
          data-transport-id="${escapeHtml(probe.transportId)}"
          ${!probe.route || probe.isBusy ? "disabled" : ""}>
          ${probe.isBusy ? "Running..." : "Run"}
        </button>
        <small>${escapeHtml(summary)}</small>
      </div>
      <pre class="runner-output ${probe.result ? "" : "is-empty"}">${escapeHtml(preview)}</pre>
    </article>`;
}

function getTransportRunnerScenarios() {
  const behaviorById = new Map((state.transports?.behaviors || []).map((behavior) => [behavior.behaviorId, behavior]));

  return TRANSPORT_RUNNER_SCENARIOS.map((scenario) => {
    const behavior = behaviorById.get(scenario.behaviorId);
    const probes = scenario.transportIds.map((transportId) => {
      const route = behavior?.routes?.find((candidate) => candidate.transportId === transportId) || null;
      return {
        transportId,
        label: TRANSPORT_LABELS[transportId] || transportId,
        route,
        result: getTransportProbeResult(scenario.id, transportId),
        isBusy: state.transportRunner.busyKeys.has(getTransportProbeKey(scenario.id, transportId))
      };
    });

    return { ...scenario, probes };
  });
}

function getTransportProbeKey(scenarioId, transportId) {
  return `${scenarioId}::${transportId}`;
}

function getTransportProbeResult(scenarioId, transportId) {
  return state.transportRunner.results[getTransportProbeKey(scenarioId, transportId)] || null;
}

async function prepareTransportRunner(options = {}) {
  const cartId = await ensureRunnerCartId();
  renderTransports();

  if (options.toastOnSuccess !== false) {
    toast(`Transport runner ready on ${cartId}.`, "success");
  }

  return { cartId };
}

async function runAllTransportScenarios() {
  const scenarios = getTransportRunnerScenarios();
  let passed = 0;
  let failed = 0;

  for (const scenario of scenarios) {
    const result = await runTransportScenario(scenario.id, { silent: true });
    passed += result.passed;
    failed += result.failed;
  }

  toast(
    failed
      ? `Browser transport validation finished with ${passed} passed and ${failed} failed probes.`
      : `Browser transport validation passed across ${passed} probes.`,
    failed ? "warning" : "success");
}

async function runTransportScenario(scenarioId, options = {}) {
  const scenario = getTransportRunnerScenarios().find((item) => item.id === scenarioId);
  if (!scenario) throw new Error("Transport scenario not found.");

  let passed = 0;
  let failed = 0;
  for (const probe of scenario.probes.filter((item) => item.route)) {
    try {
      await runTransportProbe(scenario.id, probe.transportId);
      passed += 1;
    } catch {
      failed += 1;
    }
  }

  if (!options.silent) {
    toast(
      failed
        ? `${scenario.title} finished with ${passed} passed and ${failed} failed probes.`
        : `${scenario.title} passed across ${passed} probes.`,
      failed ? "warning" : "success");
  }

  return { passed, failed };
}

async function runTransportProbe(scenarioId, transportId) {
  const scenario = TRANSPORT_RUNNER_SCENARIOS.find((item) => item.id === scenarioId);
  if (!scenario) throw new Error("Transport scenario not found.");

  const route = findBehaviorTransportRoute(scenario.behaviorId, transportId);
  if (!route) {
    throw new Error(`${TRANSPORT_LABELS[transportId] || transportId} is not published for ${scenario.behaviorId}.`);
  }

  const probeKey = getTransportProbeKey(scenarioId, transportId);
  state.transportRunner.busyKeys.add(probeKey);
  renderTransports();

  const startedAt = performance.now();
  const occurredAtUtc = new Date().toISOString();

  try {
    const input = await buildTransportScenarioInput(scenarioId);
    const payload = await executeTransportProbe(transportId, route, input);
    const result = {
      succeeded: true,
      durationMs: performance.now() - startedAt,
      occurredAtUtc,
      summary: describeTransportPayload(payload),
      preview: formatTransportPayload(payload)
    };
    state.transportRunner.results[probeKey] = result;
    state.transportRunner.lastRunAtUtc = occurredAtUtc;
    renderTransports();
    return result;
  } catch (error) {
    const failure = {
      succeeded: false,
      durationMs: performance.now() - startedAt,
      occurredAtUtc,
      summary: error.message || "Probe failed.",
      preview: error.message || "Probe failed."
    };
    state.transportRunner.results[probeKey] = failure;
    state.transportRunner.lastRunAtUtc = occurredAtUtc;
    renderTransports();
    throw error;
  } finally {
    state.transportRunner.busyKeys.delete(probeKey);
    renderTransports();
  }
}

function findBehaviorTransportRoute(behaviorId, transportId) {
  const behavior = state.transports?.behaviors?.find((item) => item.behaviorId === behaviorId);
  return behavior?.routes?.find((route) => route.transportId === transportId) || null;
}

async function buildTransportScenarioInput(scenarioId) {
  switch (scenarioId) {
    case "catalog-read":
      return {};
    case "cart-query":
      return { cartId: await ensureRunnerCartId() };
    default:
      throw new Error(`Unknown transport scenario '${scenarioId}'.`);
  }
}

async function ensureRunnerCartId() {
  if (!state.business) {
    await refreshWorkloadData();
  }

  const openCarts = (state.business?.carts || []).filter((cart) => !cart.isCheckedOut);
  const matchingCart = openCarts.find((cart) => cart.cartId === state.cartId);
  if (matchingCart) {
    state.transportRunner.lastPreparedCartId = matchingCart.cartId;
    return matchingCart.cartId;
  }

  if (openCarts.length) {
    state.cartId = openCarts[0].cartId;
    state.transportRunner.lastPreparedCartId = state.cartId;
    storeCartId(state.cartId);
    syncUrlState();
    await hydrateCartFromServer({ silent404: true });
    return state.cartId;
  }

  const product = state.business?.products?.[0];
  if (!product) {
    throw new Error("No products are available to seed the transport runner.");
  }

  createNewCart({ toastOnCreate: false });
  await addProductToCart(product.id, { toastOnSuccess: false });
  state.transportRunner.lastPreparedCartId = state.cartId;
  return state.cartId;
}

async function ensureRunnerOrderId() {
  if (!state.business) {
    await refreshWorkloadData();
  }

  const existingOrderId = state.business?.orders?.[0]?.orderId;
  if (existingOrderId) {
    state.transportRunner.lastPreparedOrderId = existingOrderId;
    return existingOrderId;
  }

  await ensureRunnerCartId();
  const createdOrder = await checkoutCartAndPlaceOrder({ toastOnSuccess: false });
  const orderId = createdOrder?.orderId || state.business?.orders?.[0]?.orderId;
  if (!orderId) {
    throw new Error("The transport runner could not prepare an order.");
  }

  state.transportRunner.lastPreparedOrderId = orderId;
  return orderId;
}

async function executeTransportProbe(transportId, route, input) {
  switch (transportId) {
    case "http.rest":
      return executeRestTransportProbe(route, input);
    case "http.graphql":
      return executeGraphqlHttpProbe(route, input);
    case "http.jsonrpc":
      return executeJsonRpcProbe(route, input);
    case "http.sse":
      return executeSseProbe(route, input);
    case "http.ws":
      return executeWebSocketProbe(route, input);
    case "http.graphql-sse":
      return executeGraphqlSseProbe(route, input);
    case "http.graphql-ws":
      return executeGraphqlWsProbe(route, input);
    default:
      throw new Error(`Unsupported probe transport '${transportId}'.`);
  }
}

async function executeRestTransportProbe(route, input) {
  const materialized = materializeRoute(route.route, input);
  const url = route.method === "GET"
    ? appendQueryString(materialized.route, materialized.remaining)
    : materialized.route;
  const response = await fetch(url, { method: route.method || "GET" });
  const payload = await readJsonOrTextResponse(response);
  if (!response.ok) {
    throw new Error(extractMessage(payload) || `${route.method} ${route.route} failed with ${response.status}.`);
  }

  return unwrapPayload(payload);
}

async function executeGraphqlHttpProbe(route, input) {
  const response = await fetch(route.route, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Accept": "application/json"
    },
    body: JSON.stringify(buildGraphqlEnvelope(input, "query"))
  });
  const payload = await readJsonOrTextResponse(response);
  if (!response.ok) {
    throw new Error(extractMessage(payload) || `GraphQL probe failed with ${response.status}.`);
  }

  if (payload?.errors?.length) {
    throw new Error(extractMessage(payload) || "GraphQL returned errors.");
  }

  return unwrapPayload(payload?.data ?? payload);
}

async function executeJsonRpcProbe(route, input) {
  const response = await fetch(route.route, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Accept": "application/json"
    },
    body: JSON.stringify({
      jsonrpc: "2.0",
      id: `probe-${Date.now()}`,
      method: "handle",
      params: input
    })
  });
  const payload = await readJsonOrTextResponse(response);
  if (!response.ok) {
    throw new Error(extractMessage(payload) || `JSON-RPC probe failed with ${response.status}.`);
  }

  if (payload?.error) {
    throw new Error(payload.error.data || payload.error.message || "JSON-RPC returned an error.");
  }

  return unwrapPayload(payload?.result ?? payload);
}

function executeSseProbe(route, input) {
  return new Promise((resolve, reject) => {
    const materialized = materializeRoute(route.route, input);
    const source = new EventSource(appendQueryString(materialized.route, materialized.remaining));
    let settled = false;
    const timer = window.setTimeout(() => fail(new Error("Timed out waiting for the SSE probe result.")), 8000);

    function cleanup() {
      window.clearTimeout(timer);
      source.close();
    }

    function succeed(payload) {
      if (settled) return;
      settled = true;
      cleanup();
      resolve(unwrapPayload(payload));
    }

    function fail(error) {
      if (settled) return;
      settled = true;
      cleanup();
      reject(error);
    }

    source.addEventListener("result", (event) => {
      try {
        succeed(JSON.parse(event.data));
      } catch {
        succeed(event.data);
      }
    });
    source.addEventListener("error", (event) => {
      if (typeof event.data === "string" && event.data.length) {
        try {
          const payload = JSON.parse(event.data);
          fail(new Error(extractMessage(payload) || "The SSE probe returned an error event."));
        } catch {
          fail(new Error(event.data));
        }
        return;
      }

      if (source.readyState === EventSource.CLOSED) {
        fail(new Error("SSE connection closed before a result event arrived."));
      }
    });
  });
}

async function executeGraphqlSseProbe(route, input) {
  const response = await fetch(route.route, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Accept": "text/event-stream"
    },
    body: JSON.stringify(buildGraphqlEnvelope(input, "query"))
  });
  const payloadText = await response.text();
  if (!response.ok) {
    throw new Error(payloadText || `GraphQL-SSE probe failed with ${response.status}.`);
  }

  const payload = parseSsePayload(payloadText, "next");
  if (payload?.errors?.length) {
    throw new Error(extractMessage(payload) || "GraphQL-SSE returned errors.");
  }

  return unwrapPayload(payload?.data ?? payload);
}

function executeWebSocketProbe(route, input) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(toWebSocketUrl(route.route));
    let settled = false;
    const timer = window.setTimeout(() => fail(new Error("Timed out waiting for the WebSocket probe response.")), 8000);

    function cleanup() {
      window.clearTimeout(timer);
      if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
        ws.close(1000, "probe complete");
      }
    }

    function succeed(payload) {
      if (settled) return;
      settled = true;
      cleanup();
      resolve(unwrapPayload(payload));
    }

    function fail(error) {
      if (settled) return;
      settled = true;
      cleanup();
      reject(error);
    }

    ws.addEventListener("open", () => {
      ws.send(JSON.stringify(input));
    });
    ws.addEventListener("message", (event) => {
      try {
        const payload = JSON.parse(typeof event.data === "string" ? event.data : "");
        if (payload?.error) {
          fail(new Error(payload.error));
          return;
        }

        succeed(payload);
      } catch {
        succeed(event.data);
      }
    });
    ws.addEventListener("error", () => fail(new Error("The WebSocket probe failed.")));
    ws.addEventListener("close", (event) => {
      if (!settled && event.code !== 1000) {
        fail(new Error(`The WebSocket probe closed with code ${event.code}.`));
      }
    });
  });
}

function executeGraphqlWsProbe(route, input) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(toWebSocketUrl(route.route), "graphql-transport-ws");
    let settled = false;
    const subscriptionId = `probe-${Date.now()}`;
    const timer = window.setTimeout(() => fail(new Error("Timed out waiting for the GraphQL-WS probe result.")), 10000);

    function cleanup() {
      window.clearTimeout(timer);
      if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
        ws.close(1000, "probe complete");
      }
    }

    function succeed(payload) {
      if (settled) return;
      settled = true;
      cleanup();
      resolve(unwrapPayload(payload));
    }

    function fail(error) {
      if (settled) return;
      settled = true;
      cleanup();
      reject(error);
    }

    ws.addEventListener("open", () => {
      ws.send(JSON.stringify({ type: "connection_init" }));
    });
    ws.addEventListener("message", (event) => {
      const raw = typeof event.data === "string" ? event.data : "";
      let message;
      try {
        message = JSON.parse(raw);
      } catch {
        fail(new Error("GraphQL-WS returned malformed JSON."));
        return;
      }

      if (message.type === "connection_ack") {
        ws.send(JSON.stringify({
          id: subscriptionId,
          type: "subscribe",
          payload: buildGraphqlEnvelope(input, "subscription")
        }));
        return;
      }

      if (message.type === "next") {
        if (message.payload?.errors?.length) {
          fail(new Error(extractMessage(message.payload) || "GraphQL-WS returned errors."));
          return;
        }

        succeed(message.payload?.data ?? message.payload);
        return;
      }

      if (message.type === "error") {
        fail(new Error(extractMessage(message.payload) || "GraphQL-WS returned errors."));
      }
    });
    ws.addEventListener("error", () => fail(new Error("The GraphQL-WS probe failed.")));
    ws.addEventListener("close", (event) => {
      if (!settled && event.code !== 1000) {
        fail(new Error(`The GraphQL-WS probe closed with code ${event.code}.`));
      }
    });
  });
}

function buildGraphqlEnvelope(input, operationKind) {
  const kind = operationKind === "subscription" ? "subscription" : "query";
  const queryName = kind === "subscription" ? "TransportProbeSubscription" : "TransportProbe";
  return {
    query: `${kind} ${queryName} { handle }`,
    variables: input
  };
}

async function readJsonOrTextResponse(response) {
  const contentType = response.headers.get("content-type") || "";
  return contentType.includes("json")
    ? response.json()
    : response.text();
}

function parseSsePayload(text, expectedEventName) {
  const chunks = String(text || "")
    .split(/\r?\n\r?\n/)
    .map((chunk) => chunk.trim())
    .filter(Boolean);

  for (const chunk of chunks) {
    let eventName = "message";
    const dataLines = [];
    for (const line of chunk.split(/\r?\n/)) {
      if (line.startsWith("event:")) {
        eventName = line.slice(6).trim();
      } else if (line.startsWith("data:")) {
        dataLines.push(line.slice(5).trimStart());
      }
    }

    if (eventName !== expectedEventName || dataLines.length === 0) {
      continue;
    }

    const payloadText = dataLines.join("\n");
    try {
      return JSON.parse(payloadText);
    } catch {
      return payloadText;
    }
  }

  throw new Error(`No '${expectedEventName}' event was found in the SSE response.`);
}

function materializeRoute(routeTemplate, input) {
  let route = routeTemplate;
  const remaining = {};

  for (const [key, value] of Object.entries(input || {})) {
    const token = `{${key}}`;
    if (route.includes(token)) {
      route = route.replaceAll(token, encodeURIComponent(String(value)));
      continue;
    }

    remaining[key] = value;
  }

  return { route, remaining };
}

function appendQueryString(path, values) {
  const entries = Object.entries(values || {}).filter(([, value]) => value !== undefined && value !== null && value !== "");
  if (!entries.length) {
    return path;
  }

  const search = new URLSearchParams();
  for (const [key, value] of entries) {
    if (Array.isArray(value)) {
      value.forEach((item) => search.append(key, String(item)));
    } else {
      search.append(key, String(value));
    }
  }

  return `${path}${path.includes("?") ? "&" : "?"}${search.toString()}`;
}

function toWebSocketUrl(path) {
  return `${window.location.origin.replace(/^http/, window.location.protocol === "https:" ? "wss" : "ws")}${path}`;
}

function describeTransportPayload(payload) {
  const value = unwrapPayload(payload);
  if (Array.isArray(value)) {
    return `${value.length} records returned`;
  }

  if (value && typeof value === "object") {
    if (Array.isArray(value.items)) {
      return `${value.items.length} items returned`;
    }

    if (value.cartId) {
      return `${value.cartId} loaded`;
    }

    if (value.orderId && value.status) {
      return `${value.orderId} is ${value.status}`;
    }

    if (value.productCount) {
      return `${value.productCount} products returned`;
    }

    return `${Object.keys(value).length} fields returned`;
  }

  return String(value ?? "No payload returned");
}

function formatTransportPayload(payload) {
  const value = unwrapPayload(payload);
  const json = typeof value === "string"
    ? value
    : (JSON.stringify(value, null, 2) ?? "null");
  return json.length > 480
    ? `${json.slice(0, 480)}\n...`
    : json;
}

function renderActivity() {
  const entries = state.activity.entries || [];
  document.getElementById("activityFeed").innerHTML = entries.length
    ? entries.map((entry) => `
      <article class="activity-item">
        <header><strong>${escapeHtml(entry.title || entry.path)}</strong><span class="status-badge ${tone(entry.outcome)}">${entry.statusCode}</span></header>
        <div class="meta-row">
          <span class="token">${escapeHtml(entry.area)}</span>
          <span class="token">${escapeHtml(entry.transport)}</span>
          <span class="token">${escapeHtml(entry.method)}</span>
          <span class="mono">${escapeHtml(entry.path)}</span>
          <span>${formatDate(entry.occurredAtUtc)}</span>
          <span>${formatDuration(entry.durationMs)}</span>
        </div>
      </article>`).join("")
    : `<div class="empty-state">No activity captured yet.</div>`;

  const byArea = countBy(entries, (entry) => entry.area);
  const byTransport = countBy(entries, (entry) => entry.transport);
  document.getElementById("activityStats").innerHTML = [
    statCard("Total recorded", state.activity.totalRecorded || 0, "all requests"),
    ...Object.entries(byArea).slice(0, 4).map(([key, value]) => statCard(key, value, "area")),
    ...Object.entries(byTransport).slice(0, 4).map(([key, value]) => statCard(key, value, "transport"))
  ].join("");
}

function renderDatabaseTopologyLinks() {
  const engine = SHOWCASE_CONFIG.engine || {};
  document.getElementById("databaseTopologyLinks").innerHTML = [
    linkButton("Showcase Projection JSON", state.summary?.documentation?.databaseTopologyPath || ENDPOINTS.systemDatabaseTopology),
    linkButton("Raw Databases", engine.databases || "/engine/databases"),
    linkButton("Database Roles", engine.databaseRoles || "/engine/database-roles"),
    linkButton("Migration Targets", engine.databaseMigrations || "/engine/database-migrations")
  ].join("");
}

function populateTransportFilters() {
  if (!state.transports) return;
  fillSelect("patternFilter", state.transports.behaviors.map((behavior) => behavior.pattern));
  fillSelect("transportFilter", state.transports.behaviors.flatMap((behavior) => behavior.transportIds));
  syncControlsFromState();
}

function fillSelect(id, values) {
  const select = document.getElementById(id);
  const current = select.value;
  const options = [...new Set(values)].sort().map((value) => `<option value="${escapeHtml(value)}">${escapeHtml(value)}</option>`).join("");
  const first = select.querySelector("option").outerHTML;
  select.innerHTML = `${first}${options}`;
  select.value = current;
}

function applyLinks() {
  const docs = SHOWCASE_CONFIG.docs || {};
  const engine = SHOWCASE_CONFIG.engine || {};
  document.getElementById("scalarLink").href = docs.scalar || "/scalar/v1";
  document.getElementById("openApiLink").href = docs.openApiJson || "/openapi/v1.json";
  document.getElementById("snapshotLink").href = engine.snapshot || "/engine/snapshot";
}

function setPill(id, text, toneClass) {
  const el = document.getElementById(id);
  el.textContent = text;
  el.className = `hero-pill ${toneClass}`;
}

function updateStreamStatus(isConnected) {
  const el = document.getElementById("streamStatus");
  el.textContent = isConnected ? "Activity stream connected" : "Activity stream reconnecting";
  el.className = `stream-status ${isConnected ? "connected" : "disconnected"}`;
}

function updateNavState() {
  const current = window.location.hash || "#overview";
  document.querySelectorAll(".section-nav a").forEach((link) => {
    link.classList.toggle("active", link.getAttribute("href") === current);
  });
}

function syncControlsFromState() {
  document.getElementById("transportSearch").value = state.filters.search || "";
  document.getElementById("patternFilter").value = state.filters.pattern || "";
  document.getElementById("transportFilter").value = state.filters.transport || "";
}

function syncUrlState() {
  const url = new URL(window.location.href);
  setSearchParam(url.searchParams, "cart", state.cartId);
  setSearchParam(url.searchParams, "q", state.filters.search);
  setSearchParam(url.searchParams, "pattern", state.filters.pattern);
  setSearchParam(url.searchParams, "transport", state.filters.transport);
  window.history.replaceState({}, "", `${url.pathname}${url.search}${url.hash}`);
}

async function requestJson(url, options = {}) {
  const response = await fetch(url, {
    method: options.method || "GET",
    headers: options.body ? { "Content-Type": "application/json" } : undefined,
    body: options.body ? JSON.stringify(options.body) : undefined
  });

  const contentType = response.headers.get("content-type") || "";
  const payload = contentType.includes("json") ? await response.json() : await response.text();
  if (!response.ok) {
    const error = new Error(extractMessage(payload) || response.statusText);
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  return payload;
}

async function loadDatabaseTopologyProjection() {
  try {
    return await requestJson(ENDPOINTS.systemDatabaseTopology);
  } catch (error) {
    toast("Database topology projection is temporarily unavailable.", "warning");
    return { errorMessage: error.message || "Database topology projection unavailable." };
  }
}

function unwrapPayload(payload) {
  return payload && typeof payload === "object" && "success" in payload && "data" in payload
    ? payload.data
    : payload;
}

function extractMessage(payload) {
  if (!payload) return "";
  if (typeof payload === "string") return payload;
  if (payload.message) return payload.message;
  if (payload.title) return payload.title;
  if (Array.isArray(payload.error) && payload.error.length) return payload.error.map((item) => item.message || item.key).join(", ");
  if (payload.error?.message) return payload.error.message;
  return "";
}

function toast(message, type) {
  const el = document.createElement("div");
  el.className = `toast toast-${type}`;
  el.textContent = message;
  document.getElementById("toastTray").appendChild(el);
  setTimeout(() => el.remove(), 3200);
}

function handleConsoleRefreshError(error) {
  toast(error.message || "Showcase console refresh failed.", "error");
}

function kpiCard(label, value, caption) {
  return `<div class="kpi-card"><span>${escapeHtml(label)}</span><strong>${escapeHtml(String(value))}</strong><small>${escapeHtml(caption)}</small></div>`;
}

function statCard(label, value, caption) {
  return `<div class="stat-card"><span>${escapeHtml(label)}</span><strong>${escapeHtml(String(value))}</strong><small>${escapeHtml(caption)}</small></div>`;
}

function metricRow(label, value) {
  return `<div><dt>${escapeHtml(label)}</dt><dd>${escapeHtml(String(value))}</dd></div>`;
}

function linkButton(label, href) {
  return `<a class="btn btn-ghost btn-sm" href="${escapeHtml(href)}">${escapeHtml(label)}</a>`;
}

function renderMetadataPreview(metadata) {
  const entries = Object.entries(metadata || {});
  if (!entries.length) {
    return "";
  }

  return entries.map(([key, value]) => `<span class="token"><strong>${escapeHtml(key)}</strong>: ${escapeHtml(String(value))}</span>`).join("");
}

function renderMetadataSections(sections, emptyMessage = "No metadata preview available.") {
  const blocks = (sections || [])
    .filter(([, metadata]) => metadata && Object.keys(metadata).length)
    .map(([label, metadata]) => `
      <div class="metadata-block">
        <span class="label">${escapeHtml(label)}</span>
        <div class="meta-row">${renderMetadataPreview(metadata)}</div>
      </div>`);

  return blocks.length
    ? `<div class="metadata-stack">${blocks.join("")}</div>`
    : `<div class="empty-inline">${escapeHtml(emptyMessage)}</div>`;
}

function renderMigrationCommandGuidance(commands) {
  return Array.isArray(commands) && commands.length
    ? `<div class="command-stack">${commands.map((command) => `
      <article class="migration-command-card">
        <header>
          <strong>${escapeHtml(command.displayName || command.id || "Command")}</strong>
          <div class="meta-row">
            ${command.id ? `<span class="token">${escapeHtml(command.id)}</span>` : ""}
            <span class="status-badge ${command.recommendedForProduction ? "status-success" : ""}">
              ${command.recommendedForProduction ? "production recommended" : "manual/local"}
            </span>
          </div>
        </header>
        <p class="command-description">${escapeHtml(command.description || "No command description published.")}</p>
        ${renderCommandVariant(
          "Run In This Sample",
          command.sampleCommand,
          command.sampleCommandHint,
          "command-snippet command-snippet-primary")}
        ${renderCommandVariant(
          command.sampleCommand && command.sampleCommand !== command.commandTemplate ? "Raw Engine Template" : "Command Template",
          command.commandTemplate,
          null,
          "command-snippet")}
        ${renderMetadataSections([
          ["Command metadata", command.metadataPreview]
        ], "No command metadata preview available.")}
      </article>`).join("")}</div>`
    : `<div class="empty-inline">No command guidance published.</div>`;
}

function renderCommandVariant(label, commandText, hint, snippetClass) {
  if (!commandText) {
    return "";
  }

  return `
    <div class="command-variant">
      <span class="label">${escapeHtml(label)}</span>
      <code class="${escapeHtml(snippetClass)}">${escapeHtml(commandText)}</code>
      ${hint ? `<small class="command-hint">${escapeHtml(hint)}</small>` : ""}
    </div>`;
}

function buildReadModelStoreRow(label, writeCount, readCount, delta) {
  return `
    <tr>
      <td><strong>${escapeHtml(label)}</strong></td>
      <td>${escapeHtml(String(writeCount))}</td>
      <td>${escapeHtml(String(readCount))}</td>
      <td><span class="status-badge ${deltaTone(delta)}">${escapeHtml(formatSignedNumber(delta))}</span></td>
    </tr>`;
}

function renderProjectionScopeCard(scope) {
  const scopeTone = scope.failedJobs > 0
    ? "status-error"
    : scope.pendingJobs > 0
      ? "status-warning"
      : "status-success";
  const scopeStatus = scope.failedJobs > 0
    ? `${scope.failedJobs} failed`
    : scope.pendingJobs > 0
      ? `${scope.pendingJobs} pending`
      : "caught up";

  return `
    <article class="scope-card">
      <header>
        <strong>${escapeHtml(scope.scope)}</strong>
        <div class="meta-row">
          <span class="status-badge ${scopeTone}">${escapeHtml(scopeStatus)}</span>
          <span class="token">max attempts ${escapeHtml(String(scope.maxAttemptCount))}</span>
        </div>
      </header>
      <div class="scope-stats">
        ${statCard("Total", scope.totalJobs, "jobs")}
        ${statCard("Completed", scope.completedJobs, "done")}
        ${statCard("Pending", scope.pendingJobs, "queued")}
        ${statCard("Failed", scope.failedJobs, "needs attention")}
      </div>
      <div class="meta-row">
        <span class="token">Next ${escapeHtml(scope.nextAvailableAtUtc ? formatDate(scope.nextAvailableAtUtc) : "none")}</span>
        <span class="token">Last completed ${escapeHtml(scope.lastCompletedAtUtc ? formatDate(scope.lastCompletedAtUtc) : "not yet")}</span>
      </div>
    </article>`;
}

function orderActions(order) {
  if (order.status === "Pending") {
    return `<button class="btn btn-sm btn-outline" data-action="reserve-order" data-order-id="${escapeHtml(order.orderId)}">Reserve</button>`;
  }
  if (order.status === "Confirmed") {
    return `<button class="btn btn-sm btn-outline" data-action="ship-order" data-order-id="${escapeHtml(order.orderId)}">Ship</button>`;
  }
  return "";
}

function tone(value) {
  const normalized = String(value || "").toLowerCase();
  if (["healthy", "started", "success", "delivered", "trusted", "allowed", "succeeded", "completed"].includes(normalized)) return "status-success";
  if (["degraded", "pending", "warning", "confirmed", "processing", "shipped", "labelcreated", "trustedonly", "partial"].includes(normalized)) return "status-warning";
  if (["unhealthy", "failed", "error", "cancelled", "conflict", "denied"].includes(normalized)) return "status-error";
  return "";
}

function deltaTone(value) {
  const numeric = Number(value || 0);
  if (numeric === 0) return "status-success";
  return numeric > 0 ? "status-warning" : "status-error";
}

function countBy(items, selector) {
  return items.reduce((map, item) => {
    const key = selector(item);
    if (!key) {
      return map;
    }

    map[key] = (map[key] || 0) + 1;
    return map;
  }, {});
}

function money(cents) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format((cents || 0) / 100);
}

function formatDate(value) {
  return new Date(value).toLocaleString("en-US", { dateStyle: "medium", timeStyle: "short" });
}

function shortId(value) {
  return value && value.length > 16 ? `${value.slice(0, 16)}...` : value;
}

function formatDuration(value) {
  return Number.isFinite(value) ? `${value.toFixed(1)} ms` : "n/a";
}

function formatSignedNumber(value) {
  const numeric = Number(value || 0);
  return numeric > 0 ? `+${numeric}` : `${numeric}`;
}

function getReadModelDeltaMagnitude(sync) {
  return [
    sync.productDelta,
    sync.inventoryDelta,
    sync.orderDelta,
    sync.shipmentDelta
  ].reduce((total, value) => total + Math.abs(Number(value || 0)), 0);
}

function hasRecommendedMigrationCommands(commands) {
  return Array.isArray(commands) && commands.some((command) => Boolean(command?.recommendedForProduction));
}

function normalizeActivityResponse(payload) {
  return {
    entries: (payload?.entries || []).map(normalizeActivityEntry),
    totalRecorded: payload?.totalRecorded || 0
  };
}

function normalizeActivityEntry(entry) {
  if (!entry || typeof entry !== "object") {
    return {};
  }

  return {
    sequence: entry.sequence ?? entry.Sequence ?? 0,
    occurredAtUtc: entry.occurredAtUtc ?? entry.OccurredAtUtc ?? null,
    area: entry.area ?? entry.Area ?? "unknown",
    transport: entry.transport ?? entry.Transport ?? "unknown",
    method: entry.method ?? entry.Method ?? "",
    path: entry.path ?? entry.Path ?? "",
    statusCode: entry.statusCode ?? entry.StatusCode ?? 0,
    durationMs: entry.durationMs ?? entry.DurationMs ?? null,
    outcome: entry.outcome ?? entry.Outcome ?? "unknown",
    title: entry.title ?? entry.Title ?? null
  };
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function shortTypeName(value) {
  const text = String(value || "");
  const lastDot = text.lastIndexOf(".");
  return lastDot >= 0 ? text.slice(lastDot + 1) : text;
}

function loadCartId() {
  try {
    const url = new URL(window.location.href);
    const cartId = url.searchParams.get("cart");
    if (cartId) {
      return cartId;
    }
  } catch {
    // Ignore URL parsing issues and fall back to persisted state.
  }

  try {
    return window.localStorage.getItem("cephalon.showcase.cartId") || `cart-${Math.random().toString(36).slice(2, 10)}`;
  } catch {
    return `cart-${Math.random().toString(36).slice(2, 10)}`;
  }
}

function storeCartId(cartId) {
  try {
    window.localStorage.setItem("cephalon.showcase.cartId", cartId);
  } catch {
    // Best effort only.
  }
}

function loadFilterState() {
  try {
    const url = new URL(window.location.href);
    return {
      search: url.searchParams.get("q")?.trim().toLowerCase() || "",
      pattern: url.searchParams.get("pattern") || "",
      transport: url.searchParams.get("transport") || ""
    };
  } catch {
    return { search: "", pattern: "", transport: "" };
  }
}

function setSearchParam(searchParams, key, value) {
  if (value) {
    searchParams.set(key, value);
    return;
  }

  searchParams.delete(key);
}

init();
