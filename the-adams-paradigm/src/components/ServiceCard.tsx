import type { Service } from '../data/services'
import { useMousePosition } from '../hooks/useMousePosition'

interface ServiceCardProps {
  service: Service
}

const currencyFormatter = new Intl.NumberFormat('en-ZA', {
  style: 'currency',
  currency: 'ZAR',
  maximumFractionDigits: 0,
})

export function ServiceCard({ service }: ServiceCardProps) {
  const { ref, position, handleMouseMove } = useMousePosition<HTMLDivElement>()
  const Icon = service.icon

  return (
    <div
      ref={ref}
      onMouseMove={handleMouseMove}
      className="group relative h-full overflow-hidden rounded-2xl border border-white/10 bg-white/[0.025] p-6 transition-all duration-300 hover:-translate-y-1 hover:border-white/20"
    >
      <div
        className="pointer-events-none absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
        style={{
          background: `radial-gradient(220px circle at ${position.x}% ${position.y}%, rgba(52,211,153,0.14), transparent 70%)`,
        }}
        aria-hidden="true"
      />
      <span className="absolute inset-x-0 top-0 h-px origin-left scale-x-0 bg-gradient-to-r from-blue-electric via-emerald-glow to-transparent transition-transform duration-500 group-hover:scale-x-100" />

      <div className="relative flex items-start justify-between gap-3">
        <div className="flex size-12 items-center justify-center rounded-xl border border-white/10 bg-navy-900/60 text-emerald-glow transition-transform duration-300 group-hover:-translate-y-0.5 group-hover:text-blue-electric">
          <Icon className="size-5" aria-hidden="true" />
        </div>
        <span className="shrink-0 rounded-full border border-white/10 bg-navy-900/60 px-3 py-1 text-xs font-semibold text-mist-100">
          {currencyFormatter.format(service.costPerHour)}
          <span className="text-mist-200/50">/hr</span>
        </span>
      </div>

      <div className="relative">
        <h3 className="mt-5 text-lg font-semibold text-mist-50">{service.title}</h3>
        <p className="mt-2 text-sm leading-relaxed text-mist-200/65">{service.description}</p>
        {service.setupFee > 0 && (
          <p className="mt-3 text-xs font-medium text-mist-200/50">
            {currencyFormatter.format(service.setupFee)} setup fee
          </p>
        )}
      </div>
    </div>
  )
}
