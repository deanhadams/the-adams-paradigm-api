import { useCallback, useEffect, useState } from 'react'
import { fallbackServiceIcon, serviceIconMap } from '../data/services'
import type { Service } from '../data/services'
import { API_BASE_URL } from '../lib/config'

interface ServiceDto {
  serviceId: number
  icon: string
  title: string
  description: string
  costPerHour: number
  setupFee: number
}

interface UseServicesResult {
  services: Service[]
  isLoading: boolean
  error: string | null
  refetch: () => void
}

export function useServices(): UseServicesResult {
  const [services, setServices] = useState<Service[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      setIsLoading(true)
      setError(null)
      try {
        const response = await fetch(`${API_BASE_URL}/api/services/get-all`, { signal: controller.signal })
        if (!response.ok) throw new Error(`Request failed with status ${response.status}`)

        const data: ServiceDto[] = await response.json()
        setServices(
          data.map((dto) => ({
            serviceId: dto.serviceId,
            icon: serviceIconMap[dto.icon] ?? fallbackServiceIcon,
            title: dto.title,
            description: dto.description,
            costPerHour: dto.costPerHour,
            setupFee: dto.setupFee,
          })),
        )
      } catch (err) {
        if (controller.signal.aborted) return
        setError(err instanceof Error ? err.message : 'Failed to load services.')
      } finally {
        if (!controller.signal.aborted) setIsLoading(false)
      }
    }

    load()
    return () => controller.abort()
  }, [reloadToken])

  const refetch = useCallback(() => setReloadToken((token) => token + 1), [])

  return { services, isLoading, error, refetch }
}
