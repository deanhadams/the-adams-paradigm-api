import { cn } from '../lib/cn'

interface LogoProps {
  className?: string
  markOnly?: boolean
}

export function LogoMark({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 40 40"
      className={cn('size-8 transition-transform duration-500 ease-out group-hover:scale-110 group-hover:-rotate-6', className)}
      aria-hidden="true"
    >
      <defs>
        <linearGradient id="paradigm-mark" x1="2" y1="4" x2="38" y2="36" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="#4d9fff" />
          <stop offset="1" stopColor="#34d399" />
        </linearGradient>
      </defs>
      <path
        d="M20 2 L36 20 L20 38 L4 20 Z"
        pathLength="100"
        strokeDasharray="100"
        fill="none"
        stroke="url(#paradigm-mark)"
        strokeWidth="2"
        strokeLinejoin="round"
        className="animate-logo-draw"
      />
      <path
        d="M20 12 L28 20 L20 28 L12 20 Z"
        fill="url(#paradigm-mark)"
        opacity="0.9"
        className="animate-spin-slow origin-center [transform-box:fill-box]"
      />
      <circle
        cx="20"
        cy="20"
        r="2.5"
        fill="#050b16"
        className="animate-pulse-soft origin-center [transform-box:fill-box]"
      />
    </svg>
  )
}

export function Logo({ className, markOnly = false }: LogoProps) {
  return (
    <span className={cn('inline-flex items-center gap-2.5', className)}>
      <LogoMark />
      {!markOnly && (
        <span className="font-display flex flex-col leading-none">
          <span className="text-[0.62rem] font-semibold uppercase tracking-[0.32em] text-emerald-glow">The</span>
          <span className="text-base font-bold tracking-tight text-mist-50">Adams Paradigm</span>
        </span>
      )}
    </span>
  )
}
