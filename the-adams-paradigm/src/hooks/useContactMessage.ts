import { useCallback, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface ContactMessageRequest {
  name: string
  email: string
  projectType: string
  budget: string
  message: string
  contextLabel?: string
}

type Status = 'idle' | 'submitting' | 'success' | 'error'

interface UseContactMessageResult {
  status: Status
  error: string | null
  sendMessage: (request: ContactMessageRequest) => Promise<void>
  reset: () => void
}

export function useContactMessage(): UseContactMessageResult {
  const [status, setStatus] = useState<Status>('idle')
  const [error, setError] = useState<string | null>(null)

  const sendMessage = useCallback(async (request: ContactMessageRequest) => {
    setStatus('submitting')
    setError(null)

    try {
      const response = await fetch(`${API_BASE_URL}/api/contact`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
      if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

      setStatus('success')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send your message.')
      setStatus('error')
    }
  }, [])

  const reset = useCallback(() => {
    setStatus('idle')
    setError(null)
  }, [])

  return { status, error, sendMessage, reset }
}
