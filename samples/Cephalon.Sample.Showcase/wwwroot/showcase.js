// ═══════════════════════════════════════════════════════════
// CephalonEngine Showcase — JavaScript
// ═══════════════════════════════════════════════════════════

const SHOWCASE_CONFIG = window.CEPHALON_SHOWCASE || {};
const ROUTE_PREFIXES = Object.freeze({
  rest: SHOWCASE_CONFIG.behaviorRoutes?.rest || '/api',
  graphql: SHOWCASE_CONFIG.behaviorRoutes?.graphql || '/graphql',
  jsonRpc: SHOWCASE_CONFIG.behaviorRoutes?.jsonRpc || '/json-rpc',
  grpc: SHOWCASE_CONFIG.behaviorRoutes?.grpc || '/grpc',
  ws: SHOWCASE_CONFIG.behaviorRoutes?.ws || '/ws',
  sse: SHOWCASE_CONFIG.behaviorRoutes?.sse || '/sse',
  graphqlWs: SHOWCASE_CONFIG.behaviorRoutes?.graphQLWs || '/graphql-ws',
  graphqlSse: SHOWCASE_CONFIG.behaviorRoutes?.graphQLSse || '/graphql-sse'
});
const API_VERSION = SHOWCASE_CONFIG.restApiVersion || 'v1';
const API = SHOWCASE_CONFIG.restApiBase || `${ROUTE_PREFIXES.rest}/${API_VERSION}/showcase`;
const BEHAVIOR_VERSION = SHOWCASE_CONFIG.behaviorVersion || 'v1';

// ── State ──────────────────────────────────────────────────
let cartId = 'cart-' + Math.random().toString(36).slice(2, 10);
let cartItems = {};
let currentStep = 0;
let lastOrderId = null;
let lastShipmentId = null;
let apiCallCount = 0;
let dashboardInterval = null;

// Connections
const sseConnections = {};
const wsConnections = {};
let labWsConn = null;
let labSseSource = null;
let labGqlWsConn = null;

// ── Utility ────────────────────────────────────────────────

function fmt(cents) { return '$' + (cents / 100).toFixed(2); }
function shortId(id) { return id?.length > 16 ? id.slice(0, 16) + '...' : id; }
function esc(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }
function ts() { return new Date().toLocaleTimeString('en-US', { hour12: false }); }
function prettyJson(obj) { try { return JSON.stringify(typeof obj === 'string' ? JSON.parse(obj) : obj, null, 2); } catch { return String(obj); } }
function behaviorPath(prefix, behavior) { return `${prefix}/${BEHAVIOR_VERSION}/${behavior.split('.').join('/')}`; }
function behaviorHttpUrl(prefix, behavior) { return behaviorPath(prefix, behavior); }
function behaviorWsUrl(prefix, behavior) {
  const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
  return `${protocol}//${location.host}${behaviorPath(prefix, behavior)}`;
}

function statusClass(s) {
  const m = {
    Pending: 'pending', Confirmed: 'confirmed', Processing: 'processing',
    Shipped: 'shipped', Delivered: 'delivered', Cancelled: 'cancelled',
    LabelCreated: 'labelcreated', InTransit: 'intransit', OutForDelivery: 'intransit', Returned: 'returned'
  };
  return 'status-' + (m[s] || 'pending');
}

function toast(msg, type = 'success') {
  const el = document.createElement('div');
  el.className = 'toast toast-' + type;
  el.textContent = msg;
  document.body.appendChild(el);
  setTimeout(() => el.remove(), 3000);
}

function setStep(n) {
  currentStep = n;
  document.querySelectorAll('.flow-step').forEach((el, i) => {
    el.classList.remove('active', 'done');
    if (i < n) el.classList.add('done');
    if (i === n) el.classList.add('active');
  });
}

// ── API Logger ─────────────────────────────────────────────

function logApi(method, url, status) {
  apiCallCount++;
  const kpi = document.getElementById('kpiApiCalls');
  if (kpi) kpi.textContent = apiCallCount;

  const log = document.getElementById('activityLog');
  if (!log) return;
  if (log.querySelector('.empty')) log.innerHTML = '';
  const ok = status >= 200 && status < 400;
  const entry = document.createElement('div');
  entry.className = 'log-entry';
  entry.innerHTML =
    `<span class="log-time">${ts()}</span>` +
    `<span class="log-method ${method.toLowerCase()}">${method}</span>` +
    `<span class="log-url">${url}</span>` +
    `<span class="log-status ${ok ? 'ok' : 'err'}">${status}</span>`;
  log.prepend(entry);
}

async function api(method, path, body) {
  const url = API + path;
  const opts = { method, headers: { 'Content-Type': 'application/json' } };
  if (body) opts.body = JSON.stringify(body);
  const res = await fetch(url, opts);
  logApi(method, url, res.status);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }
  const ct = res.headers.get('content-type') || '';
  return ct.includes('json') ? res.json() : null;
}

// ── Tab Navigation ─────────────────────────────────────────

document.querySelectorAll('.tabs .tab').forEach(tab => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.tabs .tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
    tab.classList.add('active');
    const target = document.getElementById('tab-' + tab.dataset.tab);
    if (target) target.classList.add('active');

    if (tab.dataset.tab === 'dashboard') startDashboard();
    else stopDashboard();
  });
});

// Transport sub-tabs
document.querySelectorAll('.transport-tabs .transport-tab').forEach(tab => {
  tab.addEventListener('click', () => {
    document.querySelectorAll('.transport-tabs .transport-tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.transport-panel').forEach(p => p.classList.remove('active'));
    tab.classList.add('active');
    const panel = document.getElementById(tab.dataset.transport);
    if (panel) panel.classList.add('active');
  });
});

// ═══════════════════════════════════════════════════════════
// TAB 1: E-COMMERCE FLOW
// ═══════════════════════════════════════════════════════════

// ── Catalog ────────────────────────────────────────────────

async function loadProducts() {
  try {
    const products = await api('GET', '/catalog/products');
    const tbody = document.getElementById('productList');
    if (!products.length) { tbody.innerHTML = '<tr><td colspan="4" class="empty">No products</td></tr>'; return; }
    tbody.innerHTML = products.map(p => `
      <tr>
        <td><strong>${esc(p.name)}</strong><br><span class="text-sm text-muted">${esc(p.category)}</span></td>
        <td style="font-family:var(--mono);font-size:12px;">${esc(p.sku)}</td>
        <td class="price">${fmt(p.priceInCents)}</td>
        <td><button class="btn btn-sm btn-primary" onclick="addToCart('${esc(p.id)}','${esc(p.name)}',${p.priceInCents})">+ Cart</button></td>
      </tr>`).join('');
  } catch (e) { toast('Failed to load products: ' + e.message, 'error'); }
}

// ── Cart ───────────────────────────────────────────────────

function newCart() {
  cartId = 'cart-' + Math.random().toString(36).slice(2, 10);
  cartItems = {};
  renderCart();
  document.getElementById('cartIdDisplay').textContent = cartId.slice(0, 12);
  toast('New cart created');
}

async function addToCart(productId, productName, priceInCents) {
  try {
    await api('POST', '/cart/' + cartId + '/items', {
      cartId, customerId: 'customer-web-ui', productId, productName, quantity: 1, priceInCents
    });
    cartItems[productId] = cartItems[productId] || { productId, productName, priceInCents, quantity: 0 };
    cartItems[productId].quantity++;
    renderCart();
    if (currentStep < 1) setStep(1);
    toast(productName + ' added to cart');
  } catch (e) { toast('Add to cart failed: ' + e.message, 'error'); }
}

async function removeFromCart(productId) {
  try {
    await api('DELETE', '/cart/' + cartId + '/items/' + productId);
    delete cartItems[productId];
    renderCart();
    toast('Item removed');
  } catch (e) { toast('Remove failed: ' + e.message, 'error'); }
}

function renderCart() {
  const items = Object.values(cartItems);
  const tbody = document.getElementById('cartItems');
  const btn = document.getElementById('btnCheckout');
  if (!items.length) {
    tbody.innerHTML = '<tr><td colspan="4" class="empty">Cart is empty</td></tr>';
    document.getElementById('cartTotal').textContent = '$0.00';
    btn.disabled = true;
    return;
  }
  btn.disabled = false;
  let total = 0;
  tbody.innerHTML = items.map(it => {
    const sub = it.priceInCents * it.quantity;
    total += sub;
    return `<tr>
      <td>${esc(it.productName)}</td>
      <td><span class="qty"><span>${it.quantity}</span></span></td>
      <td class="price">${fmt(sub)}</td>
      <td><button class="btn btn-sm btn-danger" onclick="removeFromCart('${esc(it.productId)}')" title="Remove">&times;</button></td>
    </tr>`;
  }).join('');
  document.getElementById('cartTotal').textContent = fmt(total);
  document.getElementById('cartIdDisplay').textContent = cartId.slice(0, 12);
}

// ── Checkout ───────────────────────────────────────────────

async function checkout() {
  try {
    await api('POST', '/cart/' + cartId + '/checkout', {
      cartId, shippingAddress: '123 Showcase Street, Demo City, DC 12345'
    });
    const items = Object.values(cartItems).map(it => ({
      productId: it.productId, productName: it.productName,
      quantity: it.quantity, unitPriceInCents: it.priceInCents
    }));
    const orderResult = await api('POST', '/orders', {
      customerId: 'customer-web-ui',
      shippingAddress: '123 Showcase Street, Demo City, DC 12345',
      items
    });
    lastOrderId = orderResult.orderId;
    if (currentStep < 3) setStep(3);
    cartItems = {};
    renderCart();
    toast('Order placed: ' + lastOrderId);
    loadOrders();
  } catch (e) { toast('Checkout failed: ' + e.message, 'error'); }
}

// ── Orders ─────────────────────────────────────────────────

async function loadOrders() {
  try {
    const orders = await api('GET', '/orders');
    const tbody = document.getElementById('orderList');
    if (!orders.length) { tbody.innerHTML = '<tr><td colspan="4" class="empty">No orders yet</td></tr>'; return; }
    tbody.innerHTML = orders.slice(0, 8).map(o => `
      <tr>
        <td style="font-family:var(--mono);font-size:12px;">${shortId(o.orderId)}</td>
        <td><span class="status ${statusClass(o.status)}">${o.status}</span></td>
        <td class="price">${fmt(o.totalInCents)}</td>
        <td>
          ${o.status === 'Pending' ? `<button class="btn btn-sm btn-success" onclick="reserveForOrder('${esc(o.orderId)}')">Reserve</button>` : ''}
          ${o.status === 'Pending' || o.status === 'Confirmed' ? `<button class="btn btn-sm btn-outline" onclick="shipOrder('${esc(o.orderId)}')">Ship</button>` : ''}
          ${o.status === 'Pending' ? `<button class="btn btn-sm btn-danger" onclick="cancelOrder('${esc(o.orderId)}')">&times;</button>` : ''}
        </td>
      </tr>`).join('');
  } catch (e) { toast('Failed to load orders: ' + e.message, 'error'); }
}

async function cancelOrder(orderId) {
  try {
    await api('PUT', '/orders/' + orderId + '/cancel', { orderId, reason: 'Cancelled from showcase UI' });
    toast('Order cancelled');
    loadOrders();
  } catch (e) { toast('Cancel failed: ' + e.message, 'error'); }
}

// ── Inventory ──────────────────────────────────────────────

async function loadInventory() {
  try {
    const items = await api('GET', '/inventory');
    const tbody = document.getElementById('inventoryList');
    if (!items.length) { tbody.innerHTML = '<tr><td colspan="4" class="empty">No inventory</td></tr>'; return; }
    tbody.innerHTML = items.map(it => `
      <tr>
        <td style="font-family:var(--mono);font-size:12px;">${shortId(it.productId)}</td>
        <td>${it.quantityOnHand}</td>
        <td>${it.quantityReserved}</td>
        <td><strong>${it.quantityAvailable}</strong></td>
      </tr>`).join('');
  } catch (e) { toast('Failed to load inventory: ' + e.message, 'error'); }
}

async function reserveForOrder(orderId) {
  try {
    const order = await api('GET', '/orders/' + orderId);
    const reserveItems = order.items.map(it => ({ productId: it.productId, quantity: it.quantity }));
    await api('POST', '/inventory/reserve', { orderId, items: reserveItems });
    if (currentStep < 4) setStep(4);
    toast('Stock reserved for ' + shortId(orderId));
    lastOrderId = orderId;
    loadInventory();
    loadOrders();
  } catch (e) { toast('Reserve failed: ' + e.message, 'error'); }
}

// ── Shipping ───────────────────────────────────────────────

async function loadShipments() {
  try {
    const shipments = await api('GET', '/shipping');
    const tbody = document.getElementById('shipmentList');
    if (!shipments.length) { tbody.innerHTML = '<tr><td colspan="4" class="empty">No shipments</td></tr>'; return; }
    tbody.innerHTML = shipments.slice(0, 8).map(s => `
      <tr>
        <td style="font-family:var(--mono);font-size:12px;">${shortId(s.shipmentId)}</td>
        <td style="font-family:var(--mono);font-size:12px;">${shortId(s.orderId)}</td>
        <td><span class="status ${statusClass(s.status)}">${s.status}</span></td>
        <td>${s.status !== 'Delivered' ? `<button class="btn btn-sm btn-success" onclick="deliverShipment('${esc(s.shipmentId)}')">Deliver</button>` : ''}</td>
      </tr>`).join('');
  } catch (e) { toast('Failed to load shipments: ' + e.message, 'error'); }
}

async function shipOrder(orderId) {
  try {
    const order = await api('GET', '/orders/' + orderId);
    const shipItems = order.items.map(it => ({ productId: it.productId, productName: it.productName, quantity: it.quantity }));
    const result = await api('POST', '/shipping', {
      orderId, destinationAddress: order.shippingAddress, items: shipItems
    });
    lastShipmentId = result.shipmentId;
    if (currentStep < 5) setStep(5);
    toast('Shipment created: ' + shortId(result.shipmentId));
    loadShipments();
    loadOrders();
  } catch (e) { toast('Ship failed: ' + e.message, 'error'); }
}

async function deliverShipment(shipmentId) {
  try {
    await api('PUT', '/shipping/' + shipmentId + '/deliver', { shipmentId, recipientName: 'Showcase Customer' });
    setStep(6);
    toast('Delivery confirmed!');
    loadShipments();
    loadOrders();
  } catch (e) { toast('Deliver failed: ' + e.message, 'error'); }
}

// ═══════════════════════════════════════════════════════════
// TAB 2: DASHBOARD
// ═══════════════════════════════════════════════════════════

function startDashboard() {
  refreshDashboard();
  if (!dashboardInterval) dashboardInterval = setInterval(refreshDashboard, 3000);
}

function stopDashboard() {
  if (dashboardInterval) { clearInterval(dashboardInterval); dashboardInterval = null; }
}

async function refreshDashboard() {
  try {
    const [products, orders, inventory, shipments] = await Promise.all([
      fetch(API + '/catalog/products').then(r => r.json()),
      fetch(API + '/orders').then(r => r.json()),
      fetch(API + '/inventory').then(r => r.json()),
      fetch(API + '/shipping').then(r => r.json())
    ]);

    // KPI values
    document.getElementById('kpiProducts').textContent = products.length;
    document.getElementById('kpiOrders').textContent = orders.length;
    document.getElementById('kpiOrdersSub').textContent = orders.length === 1 ? '1 order placed' : orders.length + ' orders placed';

    const totalRevenue = orders.reduce((s, o) => s + (o.totalInCents || 0), 0);
    document.getElementById('kpiRevenue').textContent = fmt(totalRevenue);

    document.getElementById('kpiShipments').textContent = shipments.length;
    const delivered = shipments.filter(s => s.status === 'Delivered').length;
    document.getElementById('kpiShipmentsSub').textContent = delivered + ' delivered';

    const totalReserved = inventory.reduce((s, i) => s + (i.quantityReserved || 0), 0);
    document.getElementById('kpiReserved').textContent = totalReserved;

    document.getElementById('kpiApiCalls').textContent = apiCallCount;

    // Order status breakdown
    const statusCounts = {};
    orders.forEach(o => { statusCounts[o.status] = (statusCounts[o.status] || 0) + 1; });
    const statusColors = { Pending: 'var(--yellow)', Confirmed: 'var(--accent)', Shipped: 'var(--cyan)', Delivered: 'var(--green)', Cancelled: 'var(--red)' };
    const maxStatus = Math.max(1, ...Object.values(statusCounts));
    const statusHtml = Object.entries(statusCounts).map(([status, count]) => `
      <div class="status-bar-row">
        <span class="status-bar-label">${status}</span>
        <div class="status-bar-track"><div class="status-bar-fill" style="width:${(count / maxStatus) * 100}%;background:${statusColors[status] || 'var(--text2)'}"></div></div>
        <span class="status-bar-count">${count}</span>
      </div>`).join('');
    document.getElementById('orderStatusBars').innerHTML = statusHtml || '<div class="empty">No orders yet</div>';

    // Inventory utilization
    const maxQty = Math.max(1, ...inventory.map(i => i.quantityOnHand));
    const invHtml = inventory.map(it => `
      <div class="status-bar-row">
        <span class="status-bar-label">${it.productId.replace('prod-', 'P')}</span>
        <div class="status-bar-track">
          <div class="status-bar-fill" style="width:${((it.quantityOnHand - it.quantityReserved) / maxQty) * 100}%;background:var(--green);"></div>
        </div>
        <span class="status-bar-count" title="Available / On Hand">${it.quantityOnHand - it.quantityReserved}/${it.quantityOnHand}</span>
      </div>`).join('');
    document.getElementById('inventoryBars').innerHTML = invHtml || '<div class="empty">No inventory</div>';

  } catch (e) { /* silent refresh failure */ }
}

// ═══════════════════════════════════════════════════════════
// TAB 3: LIVE TRACKING
// ═══════════════════════════════════════════════════════════

function addTimelineEvent(type, behavior, payload) {
  const timeline = document.getElementById('eventTimeline');
  if (timeline.querySelector('.empty')) timeline.innerHTML = '';
  const evt = document.createElement('div');
  evt.className = 'timeline-event evt-' + type;
  evt.innerHTML =
    `<div class="timeline-header">
      <span class="timeline-time">${ts()}</span>
      <span class="badge badge-sm badge-${type}">${type.toUpperCase()}</span>
      <span class="timeline-behavior">${behavior}</span>
    </div>
    <div class="timeline-payload">${esc(typeof payload === 'string' ? payload : JSON.stringify(payload))}</div>`;
  timeline.prepend(evt);
  // Keep max 100 events
  while (timeline.children.length > 100) timeline.removeChild(timeline.lastChild);
}

function clearTimeline() {
  document.getElementById('eventTimeline').innerHTML = '<div class="empty">Connect to SSE or WebSocket endpoints above to see live events...</div>';
}

// ── SSE Connections (Tracking) ─────────────────────────────

function connectSse() {
  const behavior = document.getElementById('sseSelect').value;
  if (sseConnections[behavior]) { toast(behavior + ' already connected', 'error'); return; }

  const url = behaviorHttpUrl(ROUTE_PREFIXES.sse, behavior);
  const source = new EventSource(url);
  sseConnections[behavior] = source;

  source.onopen = () => {
    addTimelineEvent('sse', behavior, 'Connected to ' + url);
    updateSseStatus();
  };
  source.onmessage = (event) => {
    addTimelineEvent('sse', behavior, event.data);
  };
  source.onerror = () => {
    addTimelineEvent('sse', behavior, 'Connection error / closed');
    delete sseConnections[behavior];
    updateSseStatus();
  };
  updateSseStatus();
  toast('SSE connected: ' + behavior);
}

function disconnectAllSse() {
  Object.entries(sseConnections).forEach(([id, src]) => {
    src.close();
    addTimelineEvent('sse', id, 'Disconnected');
  });
  Object.keys(sseConnections).forEach(k => delete sseConnections[k]);
  updateSseStatus();
  toast('All SSE disconnected');
}

function updateSseStatus() {
  const keys = Object.keys(sseConnections);
  const statusEl = document.getElementById('sseStatus');
  const listEl = document.getElementById('sseConnList');
  if (keys.length) {
    statusEl.innerHTML = `<span class="conn-dot connected"></span> ${keys.length} active`;
    listEl.innerHTML = keys.map(k => `<span class="badge badge-sm badge-sse" style="margin-right:4px;">${k}</span>`).join('');
  } else {
    statusEl.innerHTML = '<span class="conn-dot disconnected"></span> No connections';
    listEl.textContent = 'No active SSE connections';
  }
}

// ── WebSocket Connections (Tracking) ───────────────────────

function connectWs() {
  const behavior = document.getElementById('wsSelect').value;
  if (wsConnections[behavior]) { toast(behavior + ' already connected', 'error'); return; }

  const url = behaviorWsUrl(ROUTE_PREFIXES.ws, behavior);
  const ws = new WebSocket(url);
  wsConnections[behavior] = ws;

  ws.onopen = () => {
    addTimelineEvent('ws', behavior, 'Connected to ' + url);
    updateWsStatus();
  };
  ws.onmessage = (event) => {
    addTimelineEvent('ws', behavior, event.data);
  };
  ws.onerror = () => {
    addTimelineEvent('ws', behavior, 'Connection error');
  };
  ws.onclose = () => {
    addTimelineEvent('ws', behavior, 'Disconnected');
    delete wsConnections[behavior];
    updateWsStatus();
  };
  updateWsStatus();
  toast('WebSocket connecting: ' + behavior);
}

function disconnectAllWs() {
  Object.entries(wsConnections).forEach(([id, ws]) => { ws.close(); });
  Object.keys(wsConnections).forEach(k => delete wsConnections[k]);
  updateWsStatus();
  toast('All WebSocket disconnected');
}

function sendWsMessage() {
  const keys = Object.keys(wsConnections);
  if (!keys.length) { toast('No WebSocket connected', 'error'); return; }
  const payload = document.getElementById('wsSendPayload').value;
  keys.forEach(k => {
    const ws = wsConnections[k];
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(payload);
      addTimelineEvent('ws', k, 'SENT: ' + payload);
    }
  });
}

function updateWsStatus() {
  const keys = Object.keys(wsConnections);
  const statusEl = document.getElementById('wsStatus');
  const listEl = document.getElementById('wsConnList');
  if (keys.length) {
    statusEl.innerHTML = `<span class="conn-dot connected"></span> ${keys.length} active`;
    listEl.innerHTML = keys.map(k => `<span class="badge badge-sm badge-ws" style="margin-right:4px;">${k}</span>`).join('');
  } else {
    statusEl.innerHTML = '<span class="conn-dot disconnected"></span> No connections';
    listEl.textContent = 'No active WebSocket connections';
  }
}

// ═══════════════════════════════════════════════════════════
// TAB 4: TRANSPORT LAB
// ═══════════════════════════════════════════════════════════

// ── REST Lab ───────────────────────────────────────────────

async function labRestSend() {
  const method = document.getElementById('restMethod').value;
  const url = document.getElementById('restUrl').value;
  const body = document.getElementById('restBody').value;

  document.getElementById('restReqView').textContent = `${method} ${url}\nContent-Type: application/json\n\n${method !== 'GET' ? body : ''}`;
  try {
    const opts = { method, headers: { 'Content-Type': 'application/json' } };
    if (method !== 'GET' && method !== 'DELETE') opts.body = body;
    const res = await fetch(url, opts);
    logApi(method, url, res.status);
    const ct = res.headers.get('content-type') || '';
    const data = ct.includes('json') ? await res.json() : await res.text();
    document.getElementById('restResStatus').textContent = res.status + ' ' + res.statusText;
    document.getElementById('restResView').textContent = prettyJson(data);
  } catch (e) {
    document.getElementById('restResView').textContent = 'Error: ' + e.message;
  }
}

// ── GraphQL Lab ────────────────────────────────────────────

async function labGraphqlSend() {
  const behavior = document.getElementById('gqlBehavior').value;
  const query = document.getElementById('gqlQuery').value;
  const variables = document.getElementById('gqlVars').value;
  const url = behaviorHttpUrl(ROUTE_PREFIXES.graphql, behavior);

  const reqBody = { query, variables: JSON.parse(variables || '{}') };
  document.getElementById('gqlReqView').textContent = `POST ${url}\n\n${prettyJson(reqBody)}`;
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(reqBody)
    });
    logApi('POST', url, res.status);
    const data = await res.json();
    document.getElementById('gqlResView').textContent = prettyJson(data);
  } catch (e) {
    document.getElementById('gqlResView').textContent = 'Error: ' + e.message;
  }
}

// ── GraphQL-WS Lab ─────────────────────────────────────────

function labGqlWsConnect() {
  if (labGqlWsConn) { toast('Already connected', 'error'); return; }

  const behavior = document.getElementById('gqlWsBehavior').value;
  const url = behaviorWsUrl(ROUTE_PREFIXES.graphqlWs, behavior);

  const ws = new WebSocket(url, 'graphql-transport-ws');
  labGqlWsConn = ws;
  const logEl = document.getElementById('gqlWsLog');
  const dataEl = document.getElementById('gqlWsData');
  logEl.textContent = '';
  dataEl.textContent = '';

  function gqlWsLog(dir, msg) {
    logEl.textContent += `[${ts()}] ${dir} ${typeof msg === 'string' ? msg : JSON.stringify(msg)}\n`;
    logEl.scrollTop = logEl.scrollHeight;
  }

  ws.onopen = () => {
    updateGqlWsStatus('connected');
    // Send connection_init
    const init = { type: 'connection_init' };
    ws.send(JSON.stringify(init));
    gqlWsLog('>>>', init);
  };

  ws.onmessage = (event) => {
    const msg = JSON.parse(event.data);
    gqlWsLog('<<<', msg);

    if (msg.type === 'connection_ack') {
      // Send subscription
      const query = document.getElementById('gqlWsQuery').value;
      const sub = { id: '1', type: 'subscribe', payload: { query } };
      ws.send(JSON.stringify(sub));
      gqlWsLog('>>>', sub);
    } else if (msg.type === 'next') {
      dataEl.textContent += prettyJson(msg.payload) + '\n---\n';
      dataEl.scrollTop = dataEl.scrollHeight;
      addTimelineEvent('graphql', behavior, msg.payload);
    } else if (msg.type === 'complete') {
      gqlWsLog('---', 'Subscription complete');
    }
  };

  ws.onerror = () => { gqlWsLog('!!!', 'WebSocket error'); };
  ws.onclose = () => {
    gqlWsLog('---', 'Connection closed');
    labGqlWsConn = null;
    updateGqlWsStatus('disconnected');
  };
}

function labGqlWsDisconnect() {
  if (labGqlWsConn) {
    labGqlWsConn.close();
    labGqlWsConn = null;
  }
  updateGqlWsStatus('disconnected');
}

function updateGqlWsStatus(state) {
  const el = document.getElementById('gqlWsStatus');
  const cls = state === 'connected' ? 'connected' : 'disconnected';
  const label = state === 'connected' ? 'Connected' : 'Disconnected';
  el.innerHTML = `<span class="conn-dot ${cls}"></span> ${label}`;
}

// ── GraphQL-SSE Lab ────────────────────────────────────────

async function labGqlSseSend() {
  const behavior = document.getElementById('gqlSseBehavior').value;
  const query = document.getElementById('gqlSseQuery').value;
  const url = behaviorHttpUrl(ROUTE_PREFIXES.graphqlSse, behavior);

  const reqBody = { query };
  document.getElementById('gqlSseReqView').textContent = `POST ${url}\n\n${prettyJson(reqBody)}`;
  const resEl = document.getElementById('gqlSseResView');
  resEl.textContent = '';

  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(reqBody)
    });
    logApi('POST', url, res.status);
    const reader = res.body.getReader();
    const decoder = new TextDecoder();

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      const chunk = decoder.decode(value, { stream: true });
      resEl.textContent += chunk;
      resEl.scrollTop = resEl.scrollHeight;
    }
    resEl.textContent += '\n[Stream ended]';
  } catch (e) {
    resEl.textContent += '\nError: ' + e.message;
  }
}

// ── SSE Lab ────────────────────────────────────────────────

function labSseConnect() {
  if (labSseSource) { toast('Already connected', 'error'); return; }
  const behavior = document.getElementById('labSseBehavior').value;
  const url = behaviorHttpUrl(ROUTE_PREFIXES.sse, behavior);

  document.getElementById('labSseInfo').textContent = `Endpoint: GET ${url}\nContent-Type: text/event-stream\nBehavior: ${behavior}\nStatus: Connecting...`;
  const eventsEl = document.getElementById('labSseEvents');
  eventsEl.textContent = '';

  const source = new EventSource(url);
  labSseSource = source;

  source.onopen = () => {
    document.getElementById('labSseInfo').textContent += '\nStatus: Connected';
    updateLabSseStatus('connected');
  };
  source.onmessage = (event) => {
    eventsEl.textContent += `[${ts()}] ${event.data}\n`;
    eventsEl.scrollTop = eventsEl.scrollHeight;
    addTimelineEvent('sse', behavior, event.data);
  };
  source.onerror = () => {
    eventsEl.textContent += `[${ts()}] Connection error / closed\n`;
    labSseSource = null;
    updateLabSseStatus('disconnected');
  };
}

function labSseDisconnect() {
  if (labSseSource) { labSseSource.close(); labSseSource = null; }
  updateLabSseStatus('disconnected');
}

function updateLabSseStatus(state) {
  const el = document.getElementById('labSseStatus');
  const cls = state === 'connected' ? 'connected' : 'disconnected';
  const label = state === 'connected' ? 'Connected' : 'Disconnected';
  el.innerHTML = `<span class="conn-dot ${cls}"></span> ${label}`;
}

// ── WebSocket Lab ──────────────────────────────────────────

function labWsConnect() {
  if (labWsConn) { toast('Already connected', 'error'); return; }
  const behavior = document.getElementById('labWsBehavior').value;
  const url = behaviorWsUrl(ROUTE_PREFIXES.ws, behavior);

  const ws = new WebSocket(url);
  labWsConn = ws;

  ws.onopen = () => {
    updateLabWsStatus('connected');
    document.getElementById('labWsReceived').textContent = `[${ts()}] Connected to ${url}\n`;
  };
  ws.onmessage = (event) => {
    const el = document.getElementById('labWsReceived');
    el.textContent += `[${ts()}] ${prettyJson(event.data)}\n`;
    el.scrollTop = el.scrollHeight;
    addTimelineEvent('ws', behavior, event.data);
  };
  ws.onerror = () => {
    document.getElementById('labWsReceived').textContent += `[${ts()}] Error\n`;
  };
  ws.onclose = () => {
    document.getElementById('labWsReceived').textContent += `[${ts()}] Disconnected\n`;
    labWsConn = null;
    updateLabWsStatus('disconnected');
  };
}

function labWsDisconnect() {
  if (labWsConn) { labWsConn.close(); labWsConn = null; }
  updateLabWsStatus('disconnected');
}

function labWsSend() {
  if (!labWsConn || labWsConn.readyState !== WebSocket.OPEN) { toast('Not connected', 'error'); return; }
  const msg = document.getElementById('labWsMessage').value;
  labWsConn.send(msg);
  const el = document.getElementById('labWsSent');
  el.textContent = `[${ts()}]\n${prettyJson(msg)}`;
}

function updateLabWsStatus(state) {
  const el = document.getElementById('labWsStatus');
  const cls = state === 'connected' ? 'connected' : 'disconnected';
  const label = state === 'connected' ? 'Connected' : 'Disconnected';
  el.innerHTML = `<span class="conn-dot ${cls}"></span> ${label}`;
}

// ── JSON-RPC Lab ───────────────────────────────────────────

async function labJsonRpcSend() {
  const behavior = document.getElementById('jsonrpcBehavior').value;
  const body = document.getElementById('jsonrpcBody').value;
  const url = behaviorHttpUrl(ROUTE_PREFIXES.jsonRpc, behavior);

  document.getElementById('jsonrpcReqView').textContent = `POST ${url}\nContent-Type: application/json\n\n${prettyJson(body)}`;
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body
    });
    logApi('POST', url, res.status);
    const data = await res.json();
    document.getElementById('jsonrpcResView').textContent = prettyJson(data);
  } catch (e) {
    document.getElementById('jsonrpcResView').textContent = 'Error: ' + e.message;
  }
}

// ═══════════════════════════════════════════════════════════
// INITIALIZATION
// ═══════════════════════════════════════════════════════════

document.getElementById('cartIdDisplay').textContent = cartId.slice(0, 12);
document.getElementById('restUrl').value = `${API}/catalog/products`;
document.getElementById('labSseInfo').textContent =
  `Endpoint: GET ${behaviorHttpUrl(ROUTE_PREFIXES.sse, 'orders.get-status')}\n` +
  'Content-Type: text/event-stream\n' +
  'Protocol: Server-Sent Events (EventSource API)';
loadProducts();
loadInventory();
loadOrders();
loadShipments();
