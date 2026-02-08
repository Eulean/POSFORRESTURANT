export type DiningTable = {
  id: number
  name: string
  capacity: number
  isAvailable: boolean
}

export type OrderItem = {
  id: number
  menuItemId: number
  menuItemName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export type Order = {
  id: number
  diningTableId: number
  tableName: string
  status: string
  subtotal: number
  totalAmount: number
  createdAt: string
  updatedAt: string
  items: OrderItem[]
}

export type OrderCreateItem = {
  menuItemId: number
  quantity: number
}

export type OrderCreateRequest = {
  diningTableId: number
  items: OrderCreateItem[]
}

export type OrderStatusUpdate = {
  status: string
}
