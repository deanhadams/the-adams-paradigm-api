import { cn } from '../lib/cn'

interface TechBadgeProps {
  label: string
  className?: string
}

export function TechBadge({ label, className }: TechBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-lg border border-white/10 bg-white/[0.04] px-3 py-1.5 text-xs font-medium text-mist-100 transition-colors duration-200 hover:border-emerald-glow/40 hover:text-emerald-glow',
        className,
      )}
    >
      {label}
    </span>
  )
}
