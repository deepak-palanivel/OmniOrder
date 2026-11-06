import React, { useState } from 'react';
import { Product } from '../types';
import { createOrder } from '../api';
import { Package, ShoppingCart, Loader2, Check } from 'lucide-react';

interface InventoryTableProps {
  products: Product[];
  onOrderPlaced: () => void;
}

export const InventoryTable: React.FC<InventoryTableProps> = ({ products, onOrderPlaced }) => {
  const [buyingId, setBuyingId] = useState<string | null>(null);
  const [successId, setSuccessId] = useState<string | null>(null);

  const handleQuickBuy = async (product: Product) => {
    if (product.availableStock <= 0) return;
    setBuyingId(product.id);
    try {
      const customerId = crypto.randomUUID();
      await createOrder(customerId, product.id, 1);
      setSuccessId(product.id);
      setTimeout(() => setSuccessId(null), 1500);
      onOrderPlaced();
    } catch (err: any) {
      alert(`Order Failed: ${err.message}`);
    } finally {
      setBuyingId(null);
    }
  };

  return (
    <div className="bg-[#111827] border border-[#1F2937] rounded-xl overflow-hidden shadow-xl">
      <div className="px-5 py-4 border-b border-[#1F2937] flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <Package className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Live Inventory Ledger</h3>
            <p className="text-xs text-gray-400">Atomic Stock Tracking & Optimistic RowVersion Tokens</p>
          </div>
        </div>
        <span className="text-xs font-mono text-gray-400 bg-gray-800/60 px-2.5 py-1 rounded-md border border-gray-700">
          {products.length} Products Monitored
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-[#1F2937] bg-[#0B1120] text-[11px] font-mono uppercase text-gray-400 tracking-wider">
              <th className="py-3 px-4">SKU / Product</th>
              <th className="py-3 px-4">Price</th>
              <th className="py-3 px-4 text-center">Total</th>
              <th className="py-3 px-4 text-center">Reserved (Locked)</th>
              <th className="py-3 px-4 text-center">Available</th>
              <th className="py-3 px-4">Status & Action</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[#1F2937] text-xs">
            {products.map((p) => {
              const isGpu = p.sku === 'GPU-RTX5090';
              const isLowStock = p.availableStock <= 5;
              const isOut = p.availableStock <= 0;

              return (
                <tr
                  key={p.id}
                  className={`hover:bg-[#161F30]/60 transition ${
                    isGpu ? 'bg-amber-950/10' : ''
                  }`}
                >
                  <td className="py-3.5 px-4">
                    <div className="flex items-center gap-2">
                      <div>
                        <div className="font-semibold text-gray-200 flex items-center gap-1.5">
                          {p.name}
                          {isGpu && (
                            <span className="text-[10px] px-1.5 py-0.2 rounded bg-amber-500/20 text-amber-400 font-mono border border-amber-500/30">
                              Flash Sale
                            </span>
                          )}
                        </div>
                        <div className="text-[11px] font-mono text-gray-500 flex items-center gap-2">
                          <span>{p.sku}</span>
                          <span>•</span>
                          <span className="truncate max-w-[200px]" title={p.id}>
                            ID: {p.id.substring(0, 8)}...
                          </span>
                        </div>
                      </div>
                    </div>
                  </td>

                  <td className="py-3.5 px-4 font-mono font-medium text-gray-300">
                    ${p.price.toFixed(2)}
                  </td>

                  <td className="py-3.5 px-4 text-center font-mono text-gray-400">
                    {p.totalStock}
                  </td>

                  <td className="py-3.5 px-4 text-center font-mono">
                    <span
                      className={`px-2 py-0.5 rounded-full text-[11px] font-semibold ${
                        p.reservedStock > 0
                          ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
                          : 'text-gray-500'
                      }`}
                    >
                      {p.reservedStock}
                    </span>
                  </td>

                  <td className="py-3.5 px-4 text-center font-mono">
                    <span
                      className={`px-2.5 py-1 rounded-md text-xs font-bold ${
                        isOut
                          ? 'bg-rose-500/10 text-rose-400 border border-rose-500/20'
                          : isLowStock
                          ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
                          : 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                      }`}
                    >
                      {p.availableStock}
                    </span>
                  </td>

                  <td className="py-3.5 px-4">
                    <button
                      onClick={() => handleQuickBuy(p)}
                      disabled={isOut || buyingId === p.id}
                      className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium font-mono transition cursor-pointer disabled:cursor-not-allowed ${
                        successId === p.id
                          ? 'bg-emerald-600 text-white'
                          : isOut
                          ? 'bg-gray-800 text-gray-500 border border-gray-700'
                          : 'bg-indigo-600 hover:bg-indigo-500 text-white shadow-sm'
                      }`}
                    >
                      {buyingId === p.id ? (
                        <Loader2 className="w-3.5 h-3.5 animate-spin" />
                      ) : successId === p.id ? (
                        <Check className="w-3.5 h-3.5" />
                      ) : (
                        <ShoppingCart className="w-3.5 h-3.5" />
                      )}
                      <span>
                        {successId === p.id ? 'Reserved!' : isOut ? 'Sold Out' : 'Lock 1 Unit'}
                      </span>
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
