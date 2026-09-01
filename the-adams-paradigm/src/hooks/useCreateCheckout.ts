import { useCallback, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface CreateCheckoutRequest {
  serviceId: number
  name: string
  surname: string
  email: string
  amount: number
}

export interface CreateCheckoutResponse {
  orderId: string
  checkoutId: string | null
  paymentUrl: string | null
  amount: number
  currency: string
  yocoStatus: string | null
}

type Status = 'idle' | 'submitting' | 'success' | 'error'

interface UseCreateCheckoutResult {
  status: Status
  result: CreateCheckoutResponse | null
  error: string | null
  createCheckout: (request: CreateCheckoutRequest) => Promise<void>
  reset: () => void
}

export function useCreateCheckout(): UseCreateCheckoutResult {
  const [status, setStatus] = useState<Status>('idle')
  const [result, setResult] = useState<CreateCheckoutResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  const createCheckout = useCallback(async (request: CreateCheckoutRequest) => {
    setStatus('submitting')
    setError(null)

    try {
      const response = await fetch(`${API_BASE_URL}/api/payments/create-checkout`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
      if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

      const data: CreateCheckoutResponse = await response.json()
      setResult(data)
      setStatus('success')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create your booking.')
      setStatus('error')
    }
  }, [])

  const reset = useCallback(() => {
    setStatus('idle')
    setResult(null)
    setError(null)
  }, [])

  return { status, result, error, createCheckout, reset }
}
