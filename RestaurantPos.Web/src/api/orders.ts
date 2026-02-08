import { api } from './client'
import type { Order, OrderCreateRequest, OrderStatusUpdate } from '../types/orders'

export async function fetchOrders() {
  const response = await api.get<Order[]>('/api/orders')
  return response.data
}

export async function createOrder(payload: OrderCreateRequest) {
  const response = await api.post<Order>('/api/orders', payload)
  return response.data
}

export async function updateOrderStatus(orderId: number, payload: OrderStatusUpdate) {
  await api.put(`/api/orders/${orderId}/status`, payload)
}

export async function fetchOrderReceipt(orderId: number) {
  const response = await api.get(`/api/orders/${orderId}/receipt`, {
    responseType: 'blob'
  })
  return response.data as Blob
}
