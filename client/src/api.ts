import { DashboardMetrics, Product, Order, FlashSaleResult } from './types';

const API_BASE = '/api/v1';

export async function fetchDashboardMetrics(): Promise<DashboardMetrics> {
  const res = await fetch(`${API_BASE}/analytics/dashboard-metrics`);
  if (!res.ok) throw new Error(`Metrics error: ${res.statusText}`);
  return res.json();
}

export async function fetchProducts(): Promise<Product[]> {
  const res = await fetch(`${API_BASE}/products`);
  if (!res.ok) throw new Error(`Products error: ${res.statusText}`);
  return res.json();
}

export async function fetchOrders(): Promise<Order[]> {
  const res = await fetch(`${API_BASE}/orders`);
  if (!res.ok) throw new Error(`Orders error: ${res.statusText}`);
  return res.json();
}

export async function createOrder(customerId: string, productId: string, quantity: number): Promise<Order> {
  const idempotencyKey = crypto.randomUUID();
  const res = await fetch(`${API_BASE}/orders`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Idempotency-Key': idempotencyKey,
    },
    body: JSON.stringify({
      customerId,
      items: [{ productId, quantity }],
    }),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(errorData.detail || errorData.title || 'Failed to create order');
  }
  return res.json();
}

export async function runFlashSaleSimulation(productId?: string, buyers = 50, stock = 5): Promise<FlashSaleResult> {
  const url = new URL(`${window.location.origin}${API_BASE}/simulator/flash-sale`);
  if (productId) url.searchParams.set('productId', productId);
  url.searchParams.set('buyers', buyers.toString());
  url.searchParams.set('stock', stock.toString());

  const res = await fetch(url.toString(), { method: 'POST' });
  if (!res.ok) throw new Error(`Simulation failed: ${res.statusText}`);
  return res.json();
}

export async function generateSignedWebhook(payload: {
  eventId: string;
  eventType: string;
  orderId: string;
  amount: number;
  currency: string;
  timestamp: string;
}) {
  const res = await fetch(`${API_BASE}/webhooks/generate-signed-payload`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error(`Sign generator failed: ${res.statusText}`);
  return res.json();
}

export async function sendPaymentWebhook(
  payload: any,
  signature: string,
  idempotencyKey: string
): Promise<{ status: string; eventId: string }> {
  const res = await fetch(`${API_BASE}/webhooks/payment`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Webhook-Signature': signature,
      'X-Idempotency-Key': idempotencyKey,
    },
    body: JSON.stringify(payload),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({ detail: res.statusText }));
    throw new Error(err.detail || err.title || 'Webhook failed');
  }

  return res.json();
}

export async function getDemoToken(username = 'admin', role = 'Admin'): Promise<{ token: string; role: string }> {
  const res = await fetch(`${API_BASE}/auth/demo-token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, role }),
  });
  if (!res.ok) throw new Error(`Token error: ${res.statusText}`);
  return res.json();
}
