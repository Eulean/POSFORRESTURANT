import { api } from './client'

export type KbzPaySettings = {
  hasQr: boolean
  qrUrl: string | null
}

export async function fetchKbzPaySettings() {
  const response = await api.get<KbzPaySettings>('/api/kbzpay')
  return response.data
}

export async function uploadKbzPayQr(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  const response = await api.post<KbzPaySettings>('/api/kbzpay/qr', formData)
  return response.data
}

export async function deleteKbzPayQr() {
  await api.delete('/api/kbzpay/qr')
}

export async function startKbzPay(orderId: number) {
  await api.post(`/api/orders/${orderId}/kbzpay/start`)
}

export async function cancelKbzPay(orderId: number) {
  await api.post(`/api/orders/${orderId}/kbzpay/cancel`)
}

export async function confirmKbzPay(orderId: number, reference: string) {
  await api.post(`/api/orders/${orderId}/kbzpay/confirm`, {
    reference,
    verifiedInPartnerApp: true
  })
}
