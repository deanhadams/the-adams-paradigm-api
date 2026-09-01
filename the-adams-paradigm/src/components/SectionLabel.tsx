import { cn } from '../lib/cn'

interface SectionLabelProps {
  children: string
  className?: string
  light?: boolean
}

export function SectionLabel({ children, className, light = false }: SectionLabelProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.28em]',
        light ? 'text-navy-700' : 'text-emerald-glow',
        className,
      )}
    >
      <span className={cn('h-px w-6', light ? 'bg-navy-700' : 'bg-emerald-glow')} aria-hidden="true" />
      {children}
    </span>
  )
}
