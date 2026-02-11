import { api } from './client'
import type { ShopProfile, ShopProfileUpdateRequest } from '../types/api'

export const fetchShopProfile = async () => {
  const response = await api.get<ShopProfile>('/api/shop-profile')
  return response.data
}

export const updateShopProfile = async (payload: ShopProfileUpdateRequest) => {
  const response = await api.put<ShopProfile>('/api/shop-profile', payload)
  return response.data
}

export const uploadShopLogo = async (file: File) => {
  const formData = new FormData()
  formData.append('file', file)
  const response = await api.post<ShopProfile>('/api/shop-profile/logo', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
  return response.data
}
