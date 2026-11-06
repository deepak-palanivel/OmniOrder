import React from 'react';
import { DashboardMetrics } from '../types';
import { Zap, ShieldAlert, Activity, DollarSign, Database, Server, RefreshCw } from 'lucide-react';

interface HeaderProps {
  metrics: DashboardMetrics | null;
  isConnected: boolean;
  onRefresh: () => void;
  isLoading: boolean;
}

export const Header: React.FC<HeaderProps> = ({ metrics, isConnected, onRefresh, isLoading }) => {
  const efMs = metrics?.efCoreQueryTimeMs ?? 0;
  const dapperMs = metrics?.dapperQueryTimeMs ?? 0;
  const speedup = dapperMs > 0 ? (efMs / dapperMs).toFixed(1) : '1.0';

  return (
    <header className="border-b border-[#1F2937] bg-[#0E1526]/80 backdrop-blur-md sticky top-0 z-30 px-6 py-4">
      <div className="max-w-7xl mx-auto flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        {/* Brand & Connection Badge */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-amber-500 via-rose-500 to-indigo-600 flex items-center justify-center shadow-lg shadow-amber-500/10">
            <Zap className="w-5 h-5 text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold tracking-tight text-white flex items-center gap-1.5">
                OmniOrder
                <span className="text-xs px-2 py-0.5 rounded-full font-mono font-medium bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                  v1.0-WAL
                </span>
              </h1>
              <div className="flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-mono border bg-slate-900/60">
                <span
                  className={`w-2 h-2 rounded-full ${
                    isConnected ? 'bg-emerald-400 animate-pulse' : 'bg-rose-500'
                  }`}
                />
                <span className={isConnected ? 'text-emerald-400' : 'text-rose-400'}>
                  {isConnected ? 'SignalR Live' : 'Disconnected'}
                </span>
              </div>
            </div>
            <p className="text-xs text-gray-400">
              Distributed Locking • Anti-Overselling • Idempotent Webhooks • Dual ORM Read/Write
            </p>
          </div>
        </div>

        {/* Global Action */}
        <div className="flex items-center gap-2">
          <button
            onClick={onRefresh}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#1F2937] hover:bg-[#374151] text-xs font-medium text-gray-200 transition border border-gray-700 disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin text-indigo-400' : ''}`} />
            Sync Metrics
          </button>
        </div>
      </div>

      {/* KPI Stats Bar */}
      <div className="max-w-7xl mx-auto grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3 mt-4">
        {/* Total GMV */}
        <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-3.5 flex flex-col justify-between hover:border-gray-700 transition">
          <div className="flex items-center justify-between text-gray-400 text-xs font-medium">
            <span>Total GMV</span>
            <DollarSign className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="mt-2 text-xl font-bold font-mono text-emerald-400">
            ${metrics?.totalGMV?.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00'}
          </div>
          <div className="mt-1 text-[11px] text-gray-500 flex items-center justify-between">
            <span>{metrics?.paidOrders ?? 0} Paid</span>
            <span>{metrics?.totalOrders ?? 0} Total</span>
          </div>
        </div>

        {/* Locked Stock */}
        <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-3.5 flex flex-col justify-between hover:border-gray-700 transition">
          <div className="flex items-center justify-between text-gray-400 text-xs font-medium">
            <span>Active Locked Stock</span>
            <Activity className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-2 text-xl font-bold font-mono text-amber-400">
            {metrics?.totalLockedStock ?? 0} <span className="text-xs text-gray-500 font-normal">units</span>
          </div>
          <div className="mt-1 text-[11px] text-gray-500">
            5-min TTL Auto-Reaper Active
          </div>
        </div>

        {/* Concurrency Conflicts Blocked */}
        <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-3.5 flex flex-col justify-between hover:border-gray-700 transition">
          <div className="flex items-center justify-between text-gray-400 text-xs font-medium">
            <span>Conflicts Blocked</span>
            <ShieldAlert className="w-4 h-4 text-rose-400" />
          </div>
          <div className="mt-2 text-xl font-bold font-mono text-rose-400">
            {metrics?.concurrencyConflictsBlocked ?? 0}
          </div>
          <div className="mt-1 text-[11px] text-rose-400/80">
            409 Optimistic Collisions
          </div>
        </div>

        {/* Order Velocity */}
        <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-3.5 flex flex-col justify-between hover:border-gray-700 transition">
          <div className="flex items-center justify-between text-gray-400 text-xs font-medium">
            <span>Velocity (1h window)</span>
            <Server className="w-4 h-4 text-indigo-400" />
          </div>
          <div className="mt-2 text-xl font-bold font-mono text-indigo-300">
            {metrics?.orderVelocityPerMinute?.toFixed(2) ?? '0.00'} <span className="text-xs text-gray-500 font-normal">/min</span>
          </div>
          <div className="mt-1 text-[11px] text-gray-500">
            Rolling bounded channel
          </div>
        </div>

        {/* Dual ORM Benchmark Card */}
        <div className="col-span-2 sm:col-span-3 lg:col-span-1 bg-gradient-to-br from-[#111827] to-[#151d30] border border-indigo-500/30 rounded-xl p-3.5 flex flex-col justify-between">
          <div className="flex items-center justify-between text-xs font-medium text-indigo-300">
            <span className="flex items-center gap-1">
              <Database className="w-3.5 h-3.5" /> Dual ORM Latency
            </span>
            <span className="text-[10px] font-mono px-1.5 py-0.2 bg-indigo-500/20 text-indigo-300 rounded border border-indigo-500/30">
              {speedup}x
            </span>
          </div>
          <div className="mt-2 flex items-baseline justify-between font-mono">
            <div>
              <div className="text-[10px] text-gray-400 uppercase tracking-wider">Dapper</div>
              <div className="text-lg font-bold text-emerald-400">{dapperMs} ms</div>
            </div>
            <div className="text-right">
              <div className="text-[10px] text-gray-400 uppercase tracking-wider">EF Core</div>
              <div className="text-sm font-semibold text-gray-400">{efMs} ms</div>
            </div>
          </div>
          <div className="w-full bg-gray-800 h-1.5 rounded-full overflow-hidden mt-1 flex">
            <div
              className="bg-emerald-400 h-full transition-all duration-500"
              style={{
                width: `${Math.max(10, Math.min(90, dapperMs > 0 ? (dapperMs / (dapperMs + efMs)) * 100 : 30))}%`,
              }}
            />
            <div className="bg-indigo-500/50 h-full flex-1" />
          </div>
        </div>
      </div>
    </header>
  );
};
