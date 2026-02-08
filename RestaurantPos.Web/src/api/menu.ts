import { api } from './client'
import type { MenuItem } from '../types/api'
import type { MenuCategory, MenuItemCreateRequest } from '../types/menu'

export async function fetchMenuItems() {
  const response = await api.get<MenuItem[]>('/api/menu-items')
  return response.data
}

export async function createMenuItem(payload: MenuItemCreateRequest) {
  const response = await api.post<MenuItem>('/api/menu-items', payload)
  return response.data
}

export async function updateMenuItem(id: number, payload: MenuItemCreateRequest) {
  await api.put(`/api/menu-items/${id}`, payload)
}

export async function deleteMenuItem(id: number) {
  await api.delete(`/api/menu-items/${id}`)
}

export async function fetchMenuCategories() {
  const response = await api.get<MenuCategory[]>('/api/menu-categories')
  return response.data
}

export async function createMenuCategory(name: string) {
  const response = await api.post<MenuCategory>('/api/menu-categories', {
    name,
    isActive: true,
    sortOrder: 1
  })
  return response.data
}

export async function deleteMenuCategory(id: number) {
  await api.delete(`/api/menu-categories/${id}`)
}

export async function uploadMenuImage(menuItemId: number, file: File) {
  const formData = new FormData()
  formData.append('file', file)
  const response = await api.post<MenuItem>(`/api/menu-items/${menuItemId}/image`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data'
    }
  })
  return response.data
}
