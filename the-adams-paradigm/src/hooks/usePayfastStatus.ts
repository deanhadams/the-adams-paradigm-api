import { useCallback, useEffect, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface PayfastPaymentStatus {
  orderId: string
  orderNumber: string
  // Set server-side only after Payfast's ITN passes every security check.
  status: 'Paid' | 'Failed' | 'Cancelled' | 'Pending' | string
  amount: number
  currency: string
  paidAt: string | null
}

// 'checking': polling. 'done': a final status arrived. 'timeout': still Pending after the
// polling window. 'error': no order id, or the order doesn't exist.
type PollState = 'checking' | 'done' | 'timeout' | 'error'

const POLL_INTERVAL_MS = 3000
const POLL_WINDOW_MS = 60000
const FINAL_STATUSES = ['Paid', 'Failed', 'Cancelled']

interface UsePayfastStatusResult {
  state: PollState
  result: PayfastPaymentStatus | null
  error: string | null
  checkAgain: () => void
}

// Polls the order's status after the customer returns from Payfast. The return redirect
// itself proves nothing; only the server's status (driven by the ITN) does.
export function usePayfastStatus(orderId: string | null): UsePayfastStatusResult {
  const [state, setState] = useState<PollState>(orderId ? 'checking' : 'error')
  const [result, setResult] = useState<PayfastPaymentStatus | null>(null)
  const [error, setError] = useState<string | null>(orderId ? null : 'No order reference was found in the link.')
  const [round, setRound] = useState(0)

  useEffect(() => {
    if (!orderId) return

    let cancelled = false
    let timer: number | undefined
    const deadline = Date.now() + POLL_WINDOW_MS

    const poll = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/payments/payfast/${encodeURIComponent(orderId)}`, {
          cache: 'no-store',
        })
        if (response.status === 404) {
          if (!cancelled) {
            setError("We couldn't find this order.")
            setState('error')
          }
          return
        }
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

        const data: PayfastPaymentStatus = await response.json()
        if (cancelled) return
        setResult(data)
        if (FINAL_STATUSES.includes(data.status)) {
          setState('done')
          return
        }
      } catch (err) {
        // Transient network/server errors: keep polling until the window closes.
        console.error('Payfast status check failed:', err)
      }

      if (cancelled) return
      if (Date.now() >= deadline) {
        setState('timeout')
        return
      }
      timer = window.setTimeout(poll, POLL_INTERVAL_MS)
    }

    void poll()

    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [orderId, round])

  const checkAgain = useCallback(() => {
    setState('checking')
    setError(null)
    setRound((n) => n + 1)
  }, [])

  return { state, result, error, checkAgain }
}
