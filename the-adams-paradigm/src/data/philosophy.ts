import type { LucideIcon } from 'lucide-react'
import { Compass, Heart, Rocket, Target } from 'lucide-react'

export interface Principle {
  icon: LucideIcon
  title: string
  description: string
}

export const principles: Principle[] = [
  { icon: Target, title: 'Build With Purpose', description: 'Technology should solve a real problem.' },
  { icon: Compass, title: 'Keep It Practical', description: 'Choose architecture and tools based on the actual requirements.' },
  { icon: Heart, title: 'Make It Feel Good', description: 'Functionality and user experience should work together.' },
  { icon: Rocket, title: 'Ship & Improve', description: 'Build, test, deploy, learn and continuously improve.' },
]
