import { api } from './client'
import type { PaymentCreateRequest } from '../types/payments'

export async function addPayment(orderId: number, payload: PaymentCreateRequest) {
  await api.post(`/api/orders/${orderId}/payments`, payload)
}
