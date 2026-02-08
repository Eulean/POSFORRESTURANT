import { api } from './client'
import type { Ingredient, StockAdjustmentRequest } from '../types/inventory'

export type IngredientCreateRequest = {
  name: string
  unit: string
  stockQuantity: number
  reorderLevel: number
  costPerUnit: number
  isActive: boolean
}

export async function fetchIngredients() {
  const response = await api.get<Ingredient[]>('/api/ingredients')
  return response.data
}

export async function fetchLowStock() {
  const response = await api.get<Ingredient[]>('/api/inventory/low-stock')
  return response.data
}

export async function adjustStock(payload: StockAdjustmentRequest) {
  await api.post('/api/inventory/adjust', payload)
}

export async function createIngredient(payload: IngredientCreateRequest) {
  const response = await api.post<Ingredient>('/api/ingredients', payload)
  return response.data
}

export async function updateIngredient(id: number, payload: IngredientCreateRequest) {
  await api.put(`/api/ingredients/${id}`, payload)
}

export async function deleteIngredient(id: number) {
  await api.delete(`/api/ingredients/${id}`)
}
