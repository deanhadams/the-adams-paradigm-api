import { useEffect, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export function useAiStatus(): boolean {
  const [isOnline, setIsOnline] = useState(false)

  useEffect(() => {
    let cancelled = false

    const checkStatus = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/ai/status`)
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

        const data: { status?: string } = await response.json()

        if (!cancelled) setIsOnline(data.status === 'online')
      } catch (err) {
        console.error('AI status request failed:', err)
        if (!cancelled) setIsOnline(false)
      }
    }

    checkStatus()

    return () => {
      cancelled = true
    }
  }, [])

  return isOnline
}
