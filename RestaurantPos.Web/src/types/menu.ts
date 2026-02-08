export type MenuCategory = {
  id: number
  name: string
  isActive: boolean
  sortOrder: number
}

export type MenuItemCreateRequest = {
  name: string
  description?: string | null
  imageUrl?: string | null
  price: number
  isActive: boolean
  categoryId?: number | null
}
