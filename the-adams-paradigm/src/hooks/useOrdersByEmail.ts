import { useCallback, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface Order {
  orderNumber: string
  paymentLink: string | null
  paymentStatus: string
}

type Status = 'idle' | 'loading' | 'success' | 'error'

interface UseOrdersByEmailResult {
  status: Status
  orders: Order[]
  error: string | null
  fetchOrders: (email: string) => Promise<void>
  reset: () => void
}

export function useOrdersByEmail(): UseOrdersByEmailResult {
  const [status, setStatus] = useState<Status>('idle')
  const [orders, setOrders] = useState<Order[]>([])
  const [error, setError] = useState<string | null>(null)

  const fetchOrders = useCallback(async (email: string) => {
    setStatus('loading')
    setError(null)

    try {
      const response = await fetch(`${API_BASE_URL}/api/orders/by-email/${encodeURIComponent(email)}`)
      if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

      const data: Order[] = await response.json()
      setOrders(data)
      setStatus('success')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load bookings.')
      setStatus('error')
    }
  }, [])

  const reset = useCallback(() => {
    setStatus('idle')
    setOrders([])
    setError(null)
  }, [])

  return { status, orders, error, fetchOrders, reset }
}
