import { useCallback, useRef, useState } from 'react'

interface RelativePosition {
  x: number
  y: number
}

export function useMousePosition<T extends HTMLElement>() {
  const ref = useRef<T | null>(null)
  const [position, setPosition] = useState<RelativePosition>({ x: 50, y: 50 })

  const handleMouseMove = useCallback((event: React.MouseEvent<T>) => {
    const node = ref.current
    if (!node) return
    const rect = node.getBoundingClientRect()
    setPosition({
      x: ((event.clientX - rect.left) / rect.width) * 100,
      y: ((event.clientY - rect.top) / rect.height) * 100,
    })
  }, [])

  return { ref, position, handleMouseMove }
}
