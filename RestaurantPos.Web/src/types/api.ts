export type AuthResponse = {
  token: string
  expiresAtUtc: string
  userId: string
  userName: string
  roles: string[]
}

export type LoginRequest = {
  userName: string
  password: string
}

export type RegisterRequest = {
  userName: string
  password: string
  displayName?: string | null
  role: string
}

export type MenuItem = {
  id: number
  name: string
  description?: string | null
  imageUrl?: string | null
  price: number
  isActive: boolean
  categoryId?: number | null
}
