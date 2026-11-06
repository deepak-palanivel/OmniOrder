import React from 'react';
import { Order } from '../types';
import { Layers, Clock, CheckCircle2, XCircle, AlertCircle } from 'lucide-react';

interface OrderStreamTableProps {
  orders: Order[];
}

export const OrderStreamTable: React.FC<OrderStreamTableProps> = ({ orders }) => {
  const getStatusBadge = (status: Order['status']) => {
    switch (status) {
      case 'Paid':
      case 'Delivered':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
            <CheckCircle2 className="w-3 h-3" /> {status}
          </span>
        );
      case 'Pending':
      case 'Confirmed':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-500/20 text-amber-400 border border-amber-500/30">
            <Clock className="w-3 h-3 animate-spin" /> {status}
          </span>
        );
      case 'Expired':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-gray-500/20 text-gray-400 border border-gray-500/30">
            <AlertCircle className="w-3 h-3" /> Expired (Reaped)
          </span>
        );
      case 'Cancelled':
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-rose-500/20 text-rose-400 border border-rose-500/30">
            <XCircle className="w-3 h-3" /> Cancelled
          </span>
        );
      default:
        return (
          <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-indigo-500/20 text-indigo-400">
            {status}
          </span>
        );
    }
  };

  return (
    <div className="bg-[#111827] border border-[#1F2937] rounded-xl overflow-hidden shadow-xl">
      <div className="px-5 py-4 border-b border-[#1F2937] flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="p-2 rounded-lg bg-indigo-500/10 text-indigo-400 border border-indigo-500/20">
            <Layers className="w-4 h-4" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-white">Live Order State Stream</h3>
            <p className="text-xs text-gray-400">SignalR Outbox Processor Broadcasts & TTL Reaper</p>
          </div>
        </div>
        <span className="text-xs font-mono text-gray-400 bg-gray-800/60 px-2.5 py-1 rounded-md border border-gray-700">
          {orders.length} Recent Orders
        </span>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-[#1F2937] bg-[#0B1120] text-[11px] font-mono uppercase text-gray-400 tracking-wider">
              <th className="py-3 px-4">Order #</th>
              <th className="py-3 px-4">Customer</th>
              <th className="py-3 px-4">Items / Description</th>
              <th className="py-3 px-4">Total Amount</th>
              <th className="py-3 px-4">Status</th>
              <th className="py-3 px-4">Created (UTC)</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[#1F2937] text-xs">
            {orders.length === 0 ? (
              <tr>
                <td colSpan={6} className="py-8 text-center text-gray-500 font-mono text-xs">
                  No orders placed yet. Trigger a Flash Sale or Quick Buy above!
                </td>
              </tr>
            ) : (
              orders.slice(0, 15).map((order) => {
                const itemSummary =
                  order.items && order.items.length > 0
                    ? order.items.map((i) => `${i.quantity}x ${i.productName}`).join(', ')
                    : 'Standard Line Items';

                return (
                  <tr key={order.id} className="hover:bg-[#161F30]/60 transition">
                    <td className="py-3.5 px-4 font-mono font-bold text-indigo-300">
                      {order.orderNumber}
                    </td>

                    <td className="py-3.5 px-4 font-mono text-gray-400 text-[11px]">
                      {order.customerId.substring(0, 8)}...
                    </td>

                    <td className="py-3.5 px-4 text-gray-300 max-w-xs truncate" title={itemSummary}>
                      {itemSummary}
                    </td>

                    <td className="py-3.5 px-4 font-mono font-semibold text-emerald-400">
                      ${order.totalAmount.toFixed(2)}
                    </td>

                    <td className="py-3.5 px-4">
                      {getStatusBadge(order.status)}
                    </td>

                    <td className="py-3.5 px-4 font-mono text-gray-400 text-[11px]">
                      {new Date(order.createdAt).toLocaleTimeString()}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
