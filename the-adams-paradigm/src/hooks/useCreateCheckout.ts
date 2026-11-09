import { useCallback, useEffect, useState } from 'react'
import { CLIENT_API_KEY } from '../lib/clientApiKey'
import { API_BASE_URL } from '../lib/config'

export interface CreateCheckoutRequest {
  serviceId: number
  name: string
  surname: string
  email: string
  amount: number
  // Omitted while calendar booking is hidden in the UI — see BookingForm.tsx's
  // CALENDAR_BOOKING_ENABLED flag.
  bookingStart?: string
  durationMinutes?: number
}

export type PaymentProvider = 'Yoco' | 'Paystack'

// Normalised response from POST /api/payments/checkout. The server picks the provider
// (an admin-only setting); the customer never chooses it.
export interface CreateCheckoutResponse {
  provider: PaymentProvider
  orderId: string
  orderNumber: string
  paymentUrl: string | null
  amount: number
  currency: string
  bookingStart: string | null
  bookingEnd: string | null
}

type Status = 'idle' | 'submitting' | 'success' | 'error'

interface UseCreateCheckoutResult {
  status: Status
  result: CreateCheckoutResponse | null
  error: string | null
  createCheckout: (request: CreateCheckoutRequest) => Promise<void>
  reset: () => void
}

const PAYMENT_START_ERROR = "We couldn't start your payment. Please try again in a moment or contact us."

export function useCreateCheckout(): UseCreateCheckoutResult {
  const [status, setStatus] = useState<Status>('idle')
  const [result, setResult] = useState<CreateCheckoutResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Paystack redirects in this tab; if the customer comes Back and the browser restores
  // the page from its back/forward cache, don't leave the form stuck on "Creating booking…".
  useEffect(() => {
    const handlePageShow = (event: PageTransitionEvent) => {
      if (event.persisted) setStatus((current) => (current === 'submitting' ? 'idle' : current))
    }
    window.addEventListener('pageshow', handlePageShow)
    return () => window.removeEventListener('pageshow', handlePageShow)
  }, [])

  const createCheckout = useCallback(async (request: CreateCheckoutRequest) => {
    setStatus('submitting')
    setError(null)

    try {
      const response = await fetch(`${API_BASE_URL}/api/payments/checkout`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...request, clientApiKey: CLIENT_API_KEY }),
      })
      if (!response.ok) {
        const body = await response.json().catch(() => null) as { error?: string } | null
        throw new Error(body?.error ?? `Request failed with status ${response.status}`)
      }

      const data: CreateCheckoutResponse = await response.json()

      if (data.provider === 'Paystack') {
        if (!data.paymentUrl) throw new Error(PAYMENT_START_ERROR)
        // Same tab, so Paystack's callback redirect lands back in the customer's tab.
        // Status stays 'submitting' while the browser navigates away.
        window.location.assign(data.paymentUrl)
        return
      }

      if (data.provider !== 'Yoco') {
        // Never guess or fall back to a provider the server didn't name.
        throw new Error(PAYMENT_START_ERROR)
      }

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
