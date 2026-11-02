import { useCallback, useEffect, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface PaystackVerifyResponse {
  orderId: string
  orderNumber: string
  reference: string
  // Server-side result after verifying with Paystack — the only source of truth.
  status: 'Paid' | 'Failed' | 'Pending' | string
  paidAt: string | null
}

type Status = 'verifying' | 'done' | 'error'

interface UsePaystackVerificationResult {
  status: Status
  result: PaystackVerifyResponse | null
  error: string | null
  verify: () => void
}

export function usePaystackVerification(reference: string | null): UsePaystackVerificationResult {
  const [status, setStatus] = useState<Status>(reference ? 'verifying' : 'error')
  const [result, setResult] = useState<PaystackVerifyResponse | null>(null)
  const [error, setError] = useState<string | null>(
    reference ? null : 'No payment reference was found in the link.',
  )
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    if (!reference) return

    let cancelled = false

    const run = async () => {
      try {
        const response = await fetch(
          `${API_BASE_URL}/api/payments/paystack/verify/${encodeURIComponent(reference)}`,
        )
        if (!response.ok) {
          const body = await response.json().catch(() => null) as { error?: string } | null
          throw new Error(body?.error ?? `Request failed with status ${response.status}`)
        }

        const data: PaystackVerifyResponse = await response.json()
        if (!cancelled) {
          setResult(data)
          setStatus('done')
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'We could not confirm your payment.')
          setStatus('error')
        }
      }
    }

    void run()

    return () => {
      cancelled = true
    }
  }, [reference, attempt])

  const verify = useCallback(() => {
    setStatus('verifying')
    setError(null)
    setAttempt((n) => n + 1)
  }, [])

  return { status, result, error, verify }
}
