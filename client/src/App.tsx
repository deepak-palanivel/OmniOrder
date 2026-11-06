import { useState, useEffect, useCallback } from 'react';
import { Header } from './components/Header';
import { FlashSaleSimulator } from './components/FlashSaleSimulator';
import { WebhookTester } from './components/WebhookTester';
import { InventoryTable } from './components/InventoryTable';
import { OrderStreamTable } from './components/OrderStreamTable';
import { fetchDashboardMetrics, fetchProducts, fetchOrders, getDemoToken } from './api';
import { DashboardMetrics, Product, Order } from './types';
import { useSignalR } from './useSignalR';
import { KeyRound, Shield, Check, Copy } from 'lucide-react';

export function App() {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [jwtToken, setJwtToken] = useState<string | null>(null);
  const [copiedToken, setCopiedToken] = useState(false);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    try {
      const [m, p, o] = await Promise.all([
        fetchDashboardMetrics(),
        fetchProducts(),
        fetchOrders(),
      ]);
      setMetrics(m);
      setProducts(p);
      setOrders(o);
    } catch (err) {
      console.error('Failed to load dashboard data:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Real-time event receiver via SignalR
  const handleSignalREvent = useCallback((_event: any) => {
    loadData();
  }, [loadData]);

  const { isConnected } = useSignalR(handleSignalREvent);

  useEffect(() => {
    loadData();
    // Auto-poll fallback every 8 seconds in case SignalR reconnects
    const timer = setInterval(loadData, 8000);
    return () => clearInterval(timer);
  }, [loadData]);

  const handleGenerateToken = async (role: 'Admin' | 'Customer') => {
    try {
      const res = await getDemoToken(`demo_${role.toLowerCase()}`, role);
      setJwtToken(res.token);
    } catch (err: any) {
      alert(`Token generation failed: ${err.message}`);
    }
  };

  const copyToken = () => {
    if (!jwtToken) return;
    navigator.clipboard.writeText(jwtToken);
    setCopiedToken(true);
    setTimeout(() => setCopiedToken(false), 2000);
  };

  return (
    <div className="min-h-screen bg-[#090D16] text-gray-100 flex flex-col font-['Plus_Jakarta_Sans',sans-serif]">
      {/* Telemetry & KPI Header */}
      <Header
        metrics={metrics}
        isConnected={isConnected}
        onRefresh={loadData}
        isLoading={isLoading}
      />

      {/* Main Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-6 py-6 space-y-6">
        {/* Security & Auth Quick-Bar */}
        <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-3 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs">
          <div className="flex items-center gap-2 text-gray-400">
            <Shield className="w-4 h-4 text-indigo-400" />
            <span>RBAC & JWT Security:</span>
            <span className="text-gray-300 font-medium">Pre-seeded Admin & Customer scopes</span>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => handleGenerateToken('Admin')}
              className="px-3 py-1.5 rounded-lg bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 font-mono text-[11px] border border-indigo-500/30 flex items-center gap-1.5 cursor-pointer transition"
            >
              <KeyRound className="w-3.5 h-3.5" />
              1-Click Admin Token
            </button>
            <button
              onClick={() => handleGenerateToken('Customer')}
              className="px-3 py-1.5 rounded-lg bg-gray-800 hover:bg-gray-700 text-gray-300 font-mono text-[11px] border border-gray-700 flex items-center gap-1.5 cursor-pointer transition"
            >
              <KeyRound className="w-3.5 h-3.5" />
              Customer Token
            </button>
          </div>
        </div>

        {/* Display Generated Token Modal / Banner */}
        {jwtToken && (
          <div className="p-3 bg-[#0B1120] border border-indigo-500/40 rounded-xl flex items-center justify-between gap-3 text-xs font-mono">
            <div className="truncate flex-1">
              <span className="text-indigo-400 font-bold mr-2">JWT:</span>
              <span className="text-gray-400 select-all">{jwtToken}</span>
            </div>
            <button
              onClick={copyToken}
              className="px-2.5 py-1 rounded bg-indigo-600 hover:bg-indigo-500 text-white flex items-center gap-1 text-[11px] shrink-0 cursor-pointer"
            >
              {copiedToken ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
              {copiedToken ? 'Copied' : 'Copy'}
            </button>
          </div>
        )}

        {/* Section 1: Flash Sale Race Condition Simulator */}
        <FlashSaleSimulator onSimulationCompleted={loadData} />

        {/* Section 2: Idempotent Payment Webhook Engine */}
        <WebhookTester orders={orders} onWebhookProcessed={loadData} />

        {/* Section 3: Dual Grid (Live Inventory & Live Order Stream) */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <InventoryTable products={products} onOrderPlaced={loadData} />
          <OrderStreamTable orders={orders} />
        </div>
      </main>

      {/* Footer */}
      <footer className="border-t border-[#1F2937] py-4 px-6 text-center text-xs text-gray-500 font-mono">
        OmniOrder Architecture • EF Core 8 (Writes) + Dapper (Reads) • SQLite WAL Concurrency • SignalR Outbox Processor
      </footer>
    </div>
  );
}

export default App;
