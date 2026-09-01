import type { AnchorHTMLAttributes, ButtonHTMLAttributes, ReactNode } from 'react'
import { ArrowRight } from 'lucide-react'
import { cn } from '../lib/cn'

type Variant = 'primary' | 'secondary' | 'ghost'

const base =
  'group relative inline-flex items-center justify-center gap-2 rounded-full px-6 py-3.5 text-sm font-semibold tracking-wide transition-all duration-300 focus-visible:outline-emerald-glow disabled:cursor-not-allowed disabled:opacity-60'

const variants: Record<Variant, string> = {
  primary:
    'bg-gradient-to-r from-blue-electric to-emerald-glow text-navy-950 shadow-[0_0_0_1px_rgba(255,255,255,0.08),0_18px_40px_-12px_rgba(59,130,246,0.55)] hover:shadow-[0_0_0_1px_rgba(255,255,255,0.14),0_22px_50px_-10px_rgba(52,211,153,0.6)] hover:-translate-y-0.5',
  secondary:
    'border border-white/15 bg-white/[0.03] text-mist-50 backdrop-blur hover:border-white/30 hover:bg-white/[0.07] hover:-translate-y-0.5',
  ghost: 'text-mist-100 hover:text-emerald-glow',
}

interface CommonProps {
  children: ReactNode
  variant?: Variant
  icon?: boolean
  className?: string
}

type ButtonAsButton = CommonProps &
  ButtonHTMLAttributes<HTMLButtonElement> & {
    href?: undefined
  }

type ButtonAsAnchor = CommonProps &
  AnchorHTMLAttributes<HTMLAnchorElement> & {
    href: string
  }

type ButtonProps = ButtonAsButton | ButtonAsAnchor

export function Button({ children, variant = 'primary', icon = true, className, ...props }: ButtonProps) {
  const classes = cn(base, variants[variant], className)
  const content = (
    <>
      <span>{children}</span>
      {icon && (
        <ArrowRight
          className="size-4 shrink-0 transition-transform duration-300 group-hover:translate-x-1"
          aria-hidden="true"
        />
      )}
    </>
  )

  if ('href' in props && props.href !== undefined) {
    const { href, ...anchorProps } = props as ButtonAsAnchor
    return (
      <a href={href} className={classes} {...anchorProps}>
        {content}
      </a>
    )
  }

  const buttonProps = props as ButtonAsButton
  return (
    <button type="button" className={classes} {...buttonProps}>
      {content}
    </button>
  )
}
