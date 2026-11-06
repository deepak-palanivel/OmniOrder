export interface ProductStockInfo {
  productId: string;
  name: string;
  sku: string;
  totalStock: number;
  reservedStock: number;
  availableStock: number;
}

export interface DashboardMetrics {
  totalGMV: number;
  totalOrders: number;
  pendingOrders: number;
  paidOrders: number;
  expiredOrders: number;
  cancelledOrders: number;
  orderVelocityPerMinute: number;
  totalLockedStock: number;
  concurrencyConflictsBlocked: number;
  efCoreQueryTimeMs: number;
  dapperQueryTimeMs: number;
  productStockInfos: ProductStockInfo[];
}

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: number;
  totalStock: number;
  reservedStock: number;
  availableStock: number;
  updatedAt: string;
}

export interface OrderItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  status: 'Pending' | 'Confirmed' | 'Paid' | 'Shipped' | 'Delivered' | 'Cancelled' | 'Expired';
  totalAmount: number;
  createdAt: string;
  expiresAt: string | null;
  items: OrderItem[];
}

export interface FlashSaleAttemptResult {
  attemptNumber: number;
  success: boolean;
  orderId: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  elapsedMs: number;
}

export interface FlashSaleResult {
  totalAttempts: number;
  successfulOrders: number;
  failedAttempts: number;
  insufficientStockErrors: number;
  concurrencyConflicts: number;
  results: FlashSaleAttemptResult[];
  totalElapsedMs: number;
}

export interface WebhookLogEntry {
  id: string;
  timestamp: string;
  eventType: string;
  orderId: string;
  status: 'Success' | 'Duplicate' | 'Failed';
  message: string;
}
