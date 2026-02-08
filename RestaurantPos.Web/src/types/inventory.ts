export type Ingredient = {
  id: number
  name: string
  unit: string
  stockQuantity: number
  reorderLevel: number
  costPerUnit: number
  isActive: boolean
}

export type StockAdjustmentRequest = {
  ingredientId: number
  quantityChange: number
  reason: string
}
