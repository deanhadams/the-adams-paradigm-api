import { cn } from '../lib/cn'

interface AnimatedBackgroundProps {
  variant?: 'hero' | 'section'
  className?: string
}

export function AnimatedBackground({ variant = 'section', className }: AnimatedBackgroundProps) {
  return (
    <div className={cn('pointer-events-none absolute inset-0 overflow-hidden', className)} aria-hidden="true">
      <div className="bg-grid absolute inset-0 opacity-[0.35] [mask-image:radial-gradient(ellipse_80%_60%_at_50%_0%,black_40%,transparent_100%)]" />

      <div
        className="animate-aurora absolute -top-1/3 left-1/4 size-[52rem] rounded-full opacity-40 blur-3xl"
        style={{ background: 'radial-gradient(circle, rgba(59,130,246,0.5), transparent 65%)' }}
      />
      <div
        className="animate-aurora absolute top-1/4 right-0 size-[38rem] rounded-full opacity-30 blur-3xl [animation-delay:-6s]"
        style={{ background: 'radial-gradient(circle, rgba(52,211,153,0.45), transparent 65%)' }}
      />

      {variant === 'hero' && (
        <div
          className="animate-aurora absolute bottom-0 left-1/2 size-[40rem] -translate-x-1/2 rounded-full opacity-25 blur-3xl [animation-delay:-11s]"
          style={{ background: 'radial-gradient(circle, rgba(29,78,216,0.5), transparent 65%)' }}
        />
      )}

      <div className="bg-noise absolute inset-0" />
    </div>
  )
}
