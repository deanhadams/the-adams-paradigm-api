import type { ReactNode } from 'react'
import { cn } from '../lib/cn'
import { SectionLabel } from './SectionLabel'

interface SectionHeadingProps {
  label?: string
  title: ReactNode
  description?: ReactNode
  align?: 'left' | 'center'
  light?: boolean
  className?: string
}

export function SectionHeading({
  label,
  title,
  description,
  align = 'left',
  light = false,
  className,
}: SectionHeadingProps) {
  return (
    <div className={cn('max-w-2xl', align === 'center' && 'mx-auto text-center', className)}>
      {label && (
        <SectionLabel light={light} className={cn(align === 'center' && 'justify-center')}>
          {label}
        </SectionLabel>
      )}
      <h2
        className={cn(
          'mt-4 text-3xl font-bold leading-[1.1] sm:text-4xl lg:text-[2.75rem]',
          light ? 'text-navy-950' : 'text-mist-50',
        )}
      >
        {title}
      </h2>
      {description && (
        <p className={cn('mt-4 text-base leading-relaxed sm:text-lg', light ? 'text-navy-700' : 'text-mist-200/70')}>
          {description}
        </p>
      )}
    </div>
  )
}
