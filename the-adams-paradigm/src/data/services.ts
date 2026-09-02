import type { LucideIcon } from 'lucide-react'
import {
  Atom,
  CalendarClock,
  CloudCog,
  CreditCard,
  Database,
  Globe,
  Layers,
  Plug,
  Server,
  Sparkles,
  Wrench,
} from 'lucide-react'

export interface Service {
  serviceId: number
  icon: LucideIcon
  title: string
  description: string
  costPerHour: number
  setupFee: number
}

export const serviceIconMap: Record<string, LucideIcon> = {
  Layers,
  Wrench,
  Plug,
  Atom,
  Server,
  Database,
  CreditCard,
  CalendarClock,
  Sparkles,
  CloudCog,
  Globe,
}

export const fallbackServiceIcon: LucideIcon = Sparkles
