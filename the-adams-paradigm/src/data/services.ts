import type { LucideIcon } from 'lucide-react'
import {
  Atom,
  CalendarClock,
  CloudCog,
  CreditCard,
  Database,
  Globe,
  Layers,
  MessageCircle,
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
  isBookable: boolean
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
  MessageCircle,
}

export const fallbackServiceIcon: LucideIcon = Sparkles
