const providerInput = document.getElementById("provider");
const consumerInput = document.getElementById("consumer");
const refreshButton = document.getElementById("refresh");

refreshButton.addEventListener("click", refresh);
setInterval(refresh, 5000);
refresh();

async function refresh() {
  const provider = encodeURIComponent(providerInput.value || "demo-api");
  const consumer = encodeURIComponent(consumerInput.value || "local-demo-client");
  const [summary, burnRate, endpoints, alerts] = await Promise.all([
    getJson(`/api/dashboard/summary?provider=${provider}&consumer=${consumer}`),
    getJson(`/api/dashboard/burn-rate?provider=${provider}&consumer=${consumer}&minutes=360`),
    getJson(`/api/dashboard/endpoints?provider=${provider}&consumer=${consumer}&top=10`),
    getJson(`/api/dashboard/alerts?provider=${provider}&consumer=${consumer}`)
  ]);

  renderSummary(summary);
  renderChart(burnRate.points || []);
  renderEndpoints(endpoints.items || []);
  renderAlerts(alerts.items || []);
}

async function getJson(url) {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`Request failed: ${url}`);
  return response.json();
}

function renderSummary(summary) {
  const cards = [
    ["Quota used", summary.usagePercent == null ? "n/a" : `${summary.usagePercent.toFixed(1)}%`],
    ["Current rate", `${summary.currentRatePerMinute.toFixed(1)}/min`],
    ["Time to limit", summary.timeToLimitMinutes == null ? "n/a" : `${summary.timeToLimitMinutes.toFixed(1)} min`],
    ["Reset", summary.resetAtUtc ? new Date(summary.resetAtUtc).toLocaleString() : "n/a"],
    ["Status", summary.status || "healthy"],
    ["Recent", `${summary.currentWindowRequestCount || 0} calls`]
  ];

  document.getElementById("summary").innerHTML = cards.map(([label, value]) =>
    `<div class="card"><div class="label">${escapeHtml(label)}</div><div class="value">${escapeHtml(value)}</div></div>`
  ).join("");
}

function renderChart(points) {
  const svg = document.getElementById("chart");
  const width = 900;
  const height = 260;
  const pad = 30;
  const max = Math.max(1, ...points.map(point => point.limit || 0), ...points.map(point => point.used || 0));
  const xStep = points.length > 1 ? (width - pad * 2) / (points.length - 1) : 0;
  const y = value => height - pad - ((value || 0) / max) * (height - pad * 2);
  const line = points.map((point, index) => `${pad + index * xStep},${y(point.used)}`).join(" ");
  const limitY = y(Math.max(...points.map(point => point.limit || 0), 0));

  svg.innerHTML = `
    <line x1="${pad}" y1="${limitY}" x2="${width - pad}" y2="${limitY}" stroke="#f05b5b" stroke-dasharray="6 6" />
    <polyline fill="none" stroke="#5ba4ff" stroke-width="3" points="${line}" />
    <text x="${pad}" y="22" fill="#9ba9b6">Used quota</text>
  `;
}

function renderEndpoints(items) {
  document.getElementById("endpoints").innerHTML = items.map(item => `
    <tr>
      <td>${escapeHtml(item.method)}</td>
      <td>${escapeHtml(item.endpointTemplate)}</td>
      <td>${item.requests}</td>
      <td>${item.shareOfObservedRequestsPercent.toFixed(1)}%</td>
      <td>${item.avgLatencyMs == null ? "n/a" : item.avgLatencyMs.toFixed(1)}</td>
      <td>${item.lastStatusCode}</td>
    </tr>
  `).join("");
}

function renderAlerts(items) {
  document.getElementById("alerts").innerHTML = items.length === 0
    ? `<div class="healthy">No active alerts.</div>`
    : items.map(item => `<div class="alert ${escapeHtml(item.severity)}"><strong>${escapeHtml(item.severity)}</strong><p>${escapeHtml(item.message)}</p><small>${escapeHtml(item.recommendation)}</small></div>`).join("");
}

function escapeHtml(value) {
  return String(value).replace(/[&<>"']/g, char => ({
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    "\"": "&quot;",
    "'": "&#039;"
  })[char]);
}
