import type { LucideIcon } from 'lucide-react'
import { Building2, Lightbulb, Rocket } from 'lucide-react'

export interface ClientType {
  icon: LucideIcon
  title: string
  description: string
}

export const clientTypes: ClientType[] = [
  {
    icon: Building2,
    title: 'Small Businesses',
    description: 'Custom tools and websites that make everyday operations easier.',
  },
  {
    icon: Rocket,
    title: 'Startups',
    description: 'MVPs and scalable foundations for new products.',
  },
  {
    icon: Lightbulb,
    title: 'Individuals',
    description: 'Turning personal ideas, projects and concepts into real digital experiences.',
  },
]
