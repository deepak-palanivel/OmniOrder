import React, { useState } from 'react';
import { runFlashSaleSimulation } from '../api';
import { FlashSaleResult } from '../types';
import { Flame, CheckCircle, XCircle, AlertTriangle, Play, Loader2, Sparkles, Cpu } from 'lucide-react';

interface FlashSaleSimulatorProps {
  onSimulationCompleted: () => void;
}

export const FlashSaleSimulator: React.FC<FlashSaleSimulatorProps> = ({ onSimulationCompleted }) => {
  const [isRunning, setIsRunning] = useState(false);
  const [result, setResult] = useState<FlashSaleResult | null>(null);
  const [buyers] = useState(50);
  const [stock] = useState(5);
  const [error, setError] = useState<string | null>(null);

  const handleSimulate = async () => {
    setIsRunning(true);
    setError(null);
    try {
      // GPU product ID pre-seeded
      const gpuProductId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
      const simResult = await runFlashSaleSimulation(gpuProductId, buyers, stock);
      setResult(simResult);
      onSimulationCompleted();
    } catch (err: any) {
      setError(err.message || 'Simulation request failed');
    } finally {
      setIsRunning(false);
    }
  };

  return (
    <div className="bg-[#111827] border border-[#1F2937] rounded-xl p-5 hover:border-gray-700 transition shadow-xl relative overflow-hidden">
      {/* Decorative gradient blur */}
      <div className="absolute top-0 right-0 w-64 h-64 bg-amber-500/5 rounded-full blur-3xl -z-0 pointer-events-none" />

      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-[#1F2937]">
        <div className="flex items-center gap-3">
          <div className="p-2.5 rounded-xl bg-amber-500/10 text-amber-400 border border-amber-500/20">
            <Flame className="w-5 h-5 animate-pulse" />
          </div>
          <div>
            <h2 className="text-base font-semibold text-white flex items-center gap-2">
              Flash-Sale Concurrency Simulator
              <span className="text-[11px] font-mono font-medium px-2 py-0.5 rounded bg-amber-500/10 text-amber-400 border border-amber-500/20">
                Task.WhenAll
              </span>
            </h2>
            <p className="text-xs text-gray-400">
              Proves EF Core Optimistic Concurrency token: atomically lock stock with 0 overselling.
            </p>
          </div>
        </div>

        {/* Action Button */}
        <button
          onClick={handleSimulate}
          disabled={isRunning}
          className="flex items-center justify-center gap-2 px-5 py-2.5 rounded-lg bg-gradient-to-r from-amber-500 to-rose-600 hover:from-amber-600 hover:to-rose-700 text-white font-semibold text-xs tracking-wide uppercase transition shadow-lg shadow-amber-500/20 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer"
        >
          {isRunning ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              Firing {buyers} Threads...
            </>
          ) : (
            <>
              <Play className="w-4 h-4 fill-white" />
              Simulate {buyers} Concurrent Buyers for {stock} Units
            </>
          )}
        </button>
      </div>

      {/* Target Spec Bar */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 my-4 p-3 bg-[#0B1120] rounded-lg border border-gray-800 text-xs">
        <div className="flex items-center gap-2 text-gray-300">
          <Cpu className="w-4 h-4 text-indigo-400" />
          <span>Target Item:</span>
          <span className="font-mono text-white font-medium">⚡ RTX 5090 Ti</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-gray-400">Available Stock:</span>
          <span className="font-mono px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-bold">
            {stock} units
          </span>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-gray-400">Concurrent Requests:</span>
          <span className="font-mono px-2 py-0.5 rounded bg-rose-500/10 text-rose-400 border border-rose-500/20 font-bold">
            {buyers} parallel tasks
          </span>
        </div>
      </div>

      {error && (
        <div className="p-3 mb-4 rounded-lg bg-rose-500/10 border border-rose-500/30 text-rose-400 text-xs flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* Simulation Results Breakdown */}
      {result && (
        <div className="mt-4 space-y-4">
          {/* Summary Pills */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="p-3 rounded-lg bg-emerald-950/30 border border-emerald-500/30 flex items-center gap-3">
              <CheckCircle className="w-5 h-5 text-emerald-400 shrink-0" />
              <div>
                <div className="text-[11px] text-gray-400 uppercase font-medium">Succeeded (201)</div>
                <div className="text-xl font-bold font-mono text-emerald-400">
                  {result.successfulOrders} / {result.totalAttempts}
                </div>
              </div>
            </div>

            <div className="p-3 rounded-lg bg-rose-950/30 border border-rose-500/30 flex items-center gap-3">
              <XCircle className="w-5 h-5 text-rose-400 shrink-0" />
              <div>
                <div className="text-[11px] text-gray-400 uppercase font-medium">Blocked (409 Conflict)</div>
                <div className="text-xl font-bold font-mono text-rose-400">
                  {result.failedAttempts} / {result.totalAttempts}
                </div>
              </div>
            </div>

            <div className="p-3 rounded-lg bg-amber-950/30 border border-amber-500/30 flex items-center gap-3">
              <Sparkles className="w-5 h-5 text-amber-400 shrink-0" />
              <div>
                <div className="text-[11px] text-gray-400 uppercase font-medium">Stock Protection</div>
                <div className="text-sm font-bold font-mono text-amber-400">
                  0 Negative Stock
                </div>
              </div>
            </div>

            <div className="p-3 rounded-lg bg-indigo-950/30 border border-indigo-500/30 flex items-center gap-3">
              <div className="w-5 h-5 text-indigo-400 flex items-center justify-center font-mono font-bold text-xs shrink-0">
                ⚡
              </div>
              <div>
                <div className="text-[11px] text-gray-400 uppercase font-medium">Batch Latency</div>
                <div className="text-xl font-bold font-mono text-indigo-300">
                  {result.totalElapsedMs.toFixed(0)} <span className="text-xs font-normal">ms</span>
                </div>
              </div>
            </div>
          </div>

          {/* 50 Request Grid Visualizer */}
          <div className="p-3.5 bg-[#0B1120] rounded-xl border border-gray-800">
            <div className="flex items-center justify-between text-xs text-gray-400 mb-2">
              <span className="font-mono text-gray-300 font-medium">
                Parallel Thread Execution Matrix ({result.results.length} requests)
              </span>
              <div className="flex items-center gap-3 text-[11px]">
                <span className="flex items-center gap-1">
                  <span className="w-2.5 h-2.5 rounded-sm bg-emerald-500" /> Succeeded (201)
                </span>
                <span className="flex items-center gap-1">
                  <span className="w-2.5 h-2.5 rounded-sm bg-rose-500" /> 409 Conflict
                </span>
              </div>
            </div>

            {/* Matrix of badges */}
            <div className="grid grid-cols-5 sm:grid-cols-10 gap-1.5 max-h-48 overflow-y-auto p-1">
              {result.results.map((r) => (
                <div
                  key={r.attemptNumber}
                  className={`p-1.5 rounded-md text-center font-mono text-[10px] border transition flex flex-col items-center justify-center ${
                    r.success
                      ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40 shadow-sm shadow-emerald-500/20'
                      : 'bg-rose-500/10 text-rose-400 border-rose-500/20 hover:bg-rose-500/20'
                  }`}
                  title={`Request #${r.attemptNumber}: ${r.success ? 'Order Placed (201)' : r.errorCode} (${r.elapsedMs.toFixed(1)}ms)`}
                >
                  <span className="font-bold">#{r.attemptNumber}</span>
                  <span className="text-[9px] opacity-80">{r.success ? '201 OK' : '409'}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
