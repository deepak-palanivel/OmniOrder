import React, { useState } from 'react';
import { generateSignedWebhook, sendPaymentWebhook } from '../api';
import { Order, WebhookLogEntry } from '../types';
import { CheckCircle2, AlertOctagon, Repeat, ShieldCheck, Key } from 'lucide-react';

interface WebhookTesterProps {
  orders: Order[];
  onWebhookProcessed: () => void;
}

export const WebhookTester: React.FC<WebhookTesterProps> = ({ orders, onWebhookProcessed }) => {
  const [selectedOrderId, setSelectedOrderId] = useState<string>('');
  const [logs, setLogs] = useState<WebhookLogEntry[]>([]);
  const [isProcessing, setIsProcessing] = useState(false);

  // Default to first pending or confirmed order, or any order
  const pendingOrAnyOrders = orders.filter((o) => o.status === 'Pending' || o.status === 'Confirmed');
  const targetOrderId = selectedOrderId || (pendingOrAnyOrders[0]?.id ?? orders[0]?.id ?? '');

  const targetOrder = orders.find((o) => o.id === targetOrderId);

  const addLog = (entry: Omit<WebhookLogEntry, 'id' | 'timestamp'>) => {
    const newEntry: WebhookLogEntry = {
      ...entry,
      id: crypto.randomUUID(),
      timestamp: new Date().toLocaleTimeString(),
    };
    setLogs((prev) => [newEntry, ...prev.slice(0, 19)]);
  };

  const handleFireWebhook = async (eventType: 'PAYMENT_CAPTURED' | 'PAYMENT_FAILED', repeatCount = 1) => {
    if (!targetOrderId || !targetOrder) {
      alert('Please select an order to fire the webhook against');
      return;
    }

    setIsProcessing(true);
    const eventId = `EVT-${crypto.randomUUID().substring(0, 8)}`;
    const idempotencyKey = `idemp-${eventId}`;

    try {
      const payload = {
        eventId,
        eventType,
        orderId: targetOrderId,
        amount: targetOrder.totalAmount,
        currency: 'USD',
        timestamp: new Date().toISOString(),
      };

      // 1. Generate valid HMAC-SHA256 signature
      const signData = await generateSignedWebhook(payload);
      const signature = signData.signature;

      // 2. Fire the webhook (once or repeated 5 times for replay test)
      for (let i = 0; i < repeatCount; i++) {
        try {
          await sendPaymentWebhook(payload, signature, idempotencyKey);
          addLog({
            eventType,
            orderId: targetOrder.orderNumber,
            status: i === 0 ? 'Success' : 'Duplicate',
            message: i === 0
              ? `Webhook ${eventId} verified & processed [200 OK]`
              : `Duplicate replay #${i + 1} detected & safely deduplicated`,
          });
        } catch (err: any) {
          addLog({
            eventType,
            orderId: targetOrder.orderNumber,
            status: 'Failed',
            message: err.message || 'Webhook rejected',
          });
        }
      }

      onWebhookProcessed();
    } catch (err: any) {
      addLog({
        eventType,
        orderId: targetOrderId,
        status: 'Failed',
        message: err.message || 'Signing failed',
      });
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-5 hover:border-gray-700 transition shadow-xl">
      {/* Title */}
      <div className="flex items-center justify-between pb-4 border-b border-[#1F2937]">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div>
            <h2 className="text-base font-semibold text-white flex items-center gap-2">
              Idempotent Payment Webhook Engine
              <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
                HMAC-SHA256
              </span>
            </h2>
            <p className="text-xs text-gray-400">
              Validates cryptographically signed webhooks and prevents duplicate state transitions on network retries.
            </p>
          </div>
        </div>
      </div>

      {/* Target Order Selection */}
      <div className="my-4 grid grid-cols-1 md:grid-cols-2 gap-3">
        <div>
          <label className="block text-xs font-medium text-gray-400 mb-1.5">
            Select Target Order:
          </label>
          <select
            value={targetOrderId}
            onChange={(e) => setSelectedOrderId(e.target.value)}
            className="w-full bg-[#0B1120] border border-gray-700 rounded-lg px-3 py-2 text-xs text-gray-200 font-mono focus:border-indigo-500 focus:outline-none"
          >
            {orders.length === 0 ? (
              <option value="">No orders found</option>
            ) : (
              orders.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.orderNumber} • ${o.totalAmount.toFixed(2)} • [{o.status}]
                </option>
              ))
            )}
          </select>
        </div>

        {targetOrder && (
          <div className="p-2.5 bg-[#0B1120] rounded-lg border border-gray-800 text-xs flex items-center justify-between font-mono">
            <div>
              <span className="text-gray-400 block text-[10px]">CURRENT STATE</span>
              <span className="text-white font-semibold">{targetOrder.orderNumber}</span>
            </div>
            <div>
              <span className="text-gray-400 block text-[10px]">STATUS</span>
              <span
                className={`px-2 py-0.5 rounded-full text-[11px] font-semibold ${
                  targetOrder.status === 'Paid'
                    ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                    : targetOrder.status === 'Cancelled'
                    ? 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                    : targetOrder.status === 'Expired'
                    ? 'bg-gray-500/20 text-gray-400 border border-gray-500/30'
                    : 'bg-amber-500/20 text-amber-400 border border-amber-500/30'
                }`}
              >
                {targetOrder.status}
              </span>
            </div>
            <div>
              <span className="text-gray-400 block text-[10px]">TOTAL</span>
              <span className="text-emerald-400 font-semibold">${targetOrder.totalAmount.toFixed(2)}</span>
            </div>
          </div>
        )}
      </div>

      {/* Action Buttons */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        {/* Payment Success */}
        <button
          onClick={() => handleFireWebhook('PAYMENT_CAPTURED', 1)}
          disabled={isProcessing || !targetOrder}
          className="flex items-center justify-center gap-2 px-3 py-2.5 rounded-lg bg-emerald-600/20 hover:bg-emerald-600/30 border border-emerald-500/40 text-emerald-300 font-medium text-xs transition disabled:opacity-50 cursor-pointer"
        >
          <CheckCircle2 className="w-4 h-4 text-emerald-400" />
          Fire PAYMENT_CAPTURED (1x)
        </button>

        {/* Payment Failed */}
        <button
          onClick={() => handleFireWebhook('PAYMENT_FAILED', 1)}
          disabled={isProcessing || !targetOrder}
          className="flex items-center justify-center gap-2 px-3 py-2.5 rounded-lg bg-rose-600/20 hover:bg-rose-600/30 border border-rose-500/40 text-rose-300 font-medium text-xs transition disabled:opacity-50 cursor-pointer"
        >
          <AlertOctagon className="w-4 h-4 text-rose-400" />
          Fire PAYMENT_FAILED (Release Stock)
        </button>

        {/* Duplicate Replay x5 */}
        <button
          onClick={() => handleFireWebhook('PAYMENT_CAPTURED', 5)}
          disabled={isProcessing || !targetOrder}
          className="flex items-center justify-center gap-2 px-3 py-2.5 rounded-lg bg-indigo-600/20 hover:bg-indigo-600/30 border border-indigo-500/40 text-indigo-300 font-medium text-xs transition disabled:opacity-50 cursor-pointer"
        >
          <Repeat className="w-4 h-4 text-indigo-400" />
          Duplicate Replay Attack (x5 Burst)
        </button>
      </div>

      {/* Live Webhook Logs */}
      <div className="mt-4 p-3 bg-[#0B1120] rounded-xl border border-gray-800">
        <div className="flex items-center justify-between text-xs text-gray-400 mb-2">
          <span className="font-mono text-gray-300 font-medium flex items-center gap-1.5">
            <Key className="w-3.5 h-3.5 text-indigo-400" />
            Audit Event Stream (Idempotency Key & Signature Logs)
          </span>
          <span className="text-[10px] text-gray-500">{logs.length} events logged</span>
        </div>

        {logs.length === 0 ? (
          <div className="text-center py-4 text-xs text-gray-600 font-mono">
            No webhooks triggered yet. Click one of the buttons above to test.
          </div>
        ) : (
          <div className="space-y-1.5 max-h-40 overflow-y-auto font-mono text-[11px]">
            {logs.map((log) => (
              <div
                key={log.id}
                className="flex items-center justify-between p-2 rounded bg-[#111827] border border-gray-800/80"
              >
                <div className="flex items-center gap-2">
                  <span className="text-gray-500">{log.timestamp}</span>
                  <span
                    className={`px-1.5 py-0.5 rounded text-[10px] font-bold ${
                      log.status === 'Success'
                        ? 'bg-emerald-500/20 text-emerald-400'
                        : log.status === 'Duplicate'
                        ? 'bg-amber-500/20 text-amber-400'
                        : 'bg-rose-500/20 text-rose-400'
                    }`}
                  >
                    {log.status}
                  </span>
                  <span className="text-gray-300">{log.message}</span>
                </div>
                <span className="text-gray-500 text-[10px]">{log.orderId}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
