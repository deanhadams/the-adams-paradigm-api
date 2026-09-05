import { useCallback, useEffect, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface AvailableSlot {
  start: string
  end: string
}

type Status = 'idle' | 'loading' | 'success' | 'error'

interface UseAvailableSlotsResult {
  slots: AvailableSlot[]
  status: Status
  error: string | null
  refetch: () => void
}

function toDateParam(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function useAvailableSlots(date: Date | null, durationMinutes: number): UseAvailableSlotsResult {
  const [slots, setSlots] = useState<AvailableSlot[]>([])
  const [status, setStatus] = useState<Status>('idle')
  const [error, setError] = useState<string | null>(null)
  const [refetchToken, setRefetchToken] = useState(0)

  const refetch = useCallback(() => setRefetchToken((token) => token + 1), [])

  const dateParam = date ? toDateParam(date) : null

  useEffect(() => {
    if (!dateParam) {
      setSlots([])
      setStatus('idle')
      return
    }

    let cancelled = false
    setStatus('loading')
    setError(null)

    const params = new URLSearchParams({
      date: dateParam,
      durationMinutes: String(durationMinutes),
    })

    fetch(`${API_BASE_URL}/api/bookings/available-slots?${params.toString()}`)
      .then((response) => {
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)
        return response.json() as Promise<AvailableSlot[]>
      })
      .then((data) => {
        if (cancelled) return
        setSlots(data)
        setStatus('success')
      })
      .catch((err) => {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Failed to load available times.')
        setStatus('error')
      })

    return () => {
      cancelled = true
    }
  }, [dateParam, durationMinutes, refetchToken])

  return { slots, status, error, refetch }
}
