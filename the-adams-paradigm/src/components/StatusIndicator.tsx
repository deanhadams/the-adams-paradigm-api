import { cn } from '../lib/cn'

interface StatusIndicatorProps {
  label: string
  className?: string
}

export function StatusIndicator({ label, className }: StatusIndicatorProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-2.5 rounded-full border border-white/10 bg-white/[0.04] px-4 py-2 text-xs font-medium text-mist-100 backdrop-blur',
        className,
      )}
    >
      <span className="relative flex size-2">
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-glow opacity-75" />
        <span className="relative inline-flex size-2 rounded-full bg-emerald-glow" />
      </span>
      {label}
    </span>
  )
}
