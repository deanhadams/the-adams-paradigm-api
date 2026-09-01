import type { ReactNode } from 'react'
import { cn } from '../lib/cn'

interface ProjectVisualProps {
  slug: string
  accent: string
  className?: string
  children?: ReactNode
  image?: string
  imageAlt?: string
}

const accentMap: Record<string, { from: string; to: string; glow: string }> = {
  emerald: { from: '#34d399', to: '#1e4066', glow: 'rgba(52,211,153,0.35)' },
  blue: { from: '#4d9fff', to: '#0b1a33', glow: 'rgba(77,159,255,0.35)' },
}

function PitchPattern({ colors }: { colors: { from: string; to: string } }) {
  return (
    <svg viewBox="0 0 300 200" className="absolute inset-0 h-full w-full opacity-80" preserveAspectRatio="none">
      <rect x="20" y="20" width="260" height="160" fill="none" stroke={colors.from} strokeOpacity="0.4" strokeWidth="1.5" />
      <line x1="150" y1="20" x2="150" y2="180" stroke={colors.from} strokeOpacity="0.4" strokeWidth="1.5" />
      <circle cx="150" cy="100" r="28" fill="none" stroke={colors.from} strokeOpacity="0.4" strokeWidth="1.5" />
      <rect x="20" y="65" width="34" height="70" fill="none" stroke={colors.from} strokeOpacity="0.4" strokeWidth="1.5" />
      <rect x="246" y="65" width="34" height="70" fill="none" stroke={colors.from} strokeOpacity="0.4" strokeWidth="1.5" />
      {[
        [80, 60],
        [80, 140],
        [110, 100],
        [190, 60],
        [190, 140],
        [220, 100],
      ].map(([cx, cy]) => (
        <circle key={`${cx}-${cy}`} cx={cx} cy={cy} r="4" fill={colors.to} />
      ))}
    </svg>
  )
}

function ConstellationPattern({ colors }: { colors: { from: string; to: string } }) {
  const nodes = [
    [40, 40],
    [110, 70],
    [70, 130],
    [180, 50],
    [220, 120],
    [150, 160],
    [260, 70],
  ]
  return (
    <svg viewBox="0 0 300 200" className="absolute inset-0 h-full w-full opacity-80">
      {nodes.map(([x1, y1], i) => {
        const [x2, y2] = nodes[(i + 1) % nodes.length]
        return <line key={`l-${x1}-${y1}`} x1={x1} y1={y1} x2={x2} y2={y2} stroke={colors.from} strokeOpacity="0.35" strokeWidth="1" />
      })}
      {nodes.map(([x, y], i) => (
        <circle key={`n-${x}-${y}`} cx={x} cy={y} r={i % 2 === 0 ? 4 : 2.5} fill={colors.from} />
      ))}
    </svg>
  )
}

function GridPulsePattern({ colors }: { colors: { from: string; to: string } }) {
  return (
    <svg viewBox="0 0 300 200" className="absolute inset-0 h-full w-full opacity-80">
      {Array.from({ length: 6 }).map((_, row) =>
        Array.from({ length: 9 }).map((_, col) => (
          <rect
            key={`${row}-${col}`}
            x={10 + col * 32}
            y={10 + row * 32}
            width="22"
            height="22"
            rx="4"
            fill={(row + col) % 5 === 0 ? colors.from : colors.to}
            fillOpacity={(row + col) % 5 === 0 ? 0.55 : 0.25}
          />
        )),
      )}
    </svg>
  )
}

function LayeredPanelsPattern({ colors }: { colors: { from: string; to: string } }) {
  return (
    <svg viewBox="0 0 300 200" className="absolute inset-0 h-full w-full opacity-80">
      <rect x="24" y="30" width="150" height="34" rx="6" fill="none" stroke={colors.from} strokeOpacity="0.45" strokeWidth="1.5" />
      <rect x="24" y="76" width="252" height="94" rx="8" fill="none" stroke={colors.from} strokeOpacity="0.45" strokeWidth="1.5" />
      <rect x="40" y="94" width="90" height="12" rx="3" fill={colors.from} fillOpacity="0.4" />
      <rect x="40" y="114" width="140" height="12" rx="3" fill={colors.from} fillOpacity="0.25" />
      <rect x="40" y="134" width="60" height="12" rx="3" fill={colors.from} fillOpacity="0.25" />
      <circle cx="240" cy="122" r="24" fill="none" stroke={colors.from} strokeOpacity="0.5" strokeWidth="1.5" />
    </svg>
  )
}

const patterns: Record<string, typeof PitchPattern> = {
  'complete-the-eleven-fc': PitchPattern,
  'devil-bunny': ConstellationPattern,
  'world-quiz-league': GridPulsePattern,
  flowdesk: LayeredPanelsPattern,
}

export function ProjectVisual({ slug, accent, className, children, image, imageAlt }: ProjectVisualProps) {
  const colors = accentMap[accent] ?? accentMap.blue

  if (image) {
    return (
      <div className={cn('relative overflow-hidden bg-navy-900', className)}>
        <img
          src={image}
          alt={imageAlt ?? ''}
          loading="lazy"
          className="absolute inset-0 h-full w-full object-cover object-top transition-transform duration-500 group-hover:scale-105"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-navy-950 via-navy-950/10 to-transparent" aria-hidden="true" />
        {children}
      </div>
    )
  }

  const Pattern = patterns[slug] ?? ConstellationPattern

  return (
    <div className={cn('relative overflow-hidden bg-navy-900', className)}>
      <div
        className="absolute inset-0"
        style={{ background: `radial-gradient(120% 120% at 20% 15%, ${colors.glow}, transparent 60%)` }}
        aria-hidden="true"
      />
      <div className="bg-grid absolute inset-0 opacity-40" aria-hidden="true" />
      <Pattern colors={colors} />
      <div className="absolute inset-0 bg-gradient-to-t from-navy-950 via-transparent to-transparent" aria-hidden="true" />
      {children}
    </div>
  )
}
