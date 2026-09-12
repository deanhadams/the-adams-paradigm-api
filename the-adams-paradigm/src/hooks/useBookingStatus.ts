import { useEffect, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export function useBookingStatus(): boolean {
  const [isEnabled, setIsEnabled] = useState(true)

  useEffect(() => {
    let cancelled = false

    const checkStatus = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/bookings/status`)
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

        const data: { enabled?: boolean } = await response.json()

        if (!cancelled) setIsEnabled(data.enabled ?? true)
      } catch (err) {
        console.error('Booking status request failed:', err)
        // Fail open — don't hide the booking flow just because the status check failed.
      }
    }

    checkStatus()

    return () => {
      cancelled = true
    }
  }, [])

  return isEnabled
}
