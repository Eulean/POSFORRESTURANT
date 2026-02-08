export type PaymentCreateRequest = {
  amount: number
  method: string
  reference?: string | null
}
