import { AlertCircle } from 'lucide-react'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'
import { ServiceCard } from '../components/ServiceCard'
import { useServices } from '../hooks/useServices'

export function Services() {
  const { services, isLoading, error, refetch } = useServices()

  return (
    <section id="services" className="relative py-28 lg:py-36">
      <div className="mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading
            label="What I Build"
            title="Turning complex ideas into useful, scalable digital products."
          />
        </RevealOnScroll>

        {isLoading && (
          <div className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-3" aria-busy="true" aria-live="polite">
            {Array.from({ length: 6 }).map((_, index) => (
              <div
                key={index}
                className="h-48 animate-pulse rounded-2xl border border-white/10 bg-white/[0.06]"
              />
            ))}
          </div>
        )}

        {!isLoading && error && (
          <div className="mt-14 flex flex-col items-center gap-4 rounded-2xl border border-red-400/20 bg-red-400/5 px-8 py-14 text-center">
            <AlertCircle className="size-8 text-red-400" aria-hidden="true" />
            <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
              Couldn't load services right now. {error}
            </p>
            <button
              type="button"
              onClick={refetch}
              className="text-sm font-semibold text-emerald-glow hover:underline"
            >
              Try again
            </button>
          </div>
        )}

        {!isLoading && !error && services.length > 0 && (
          <div className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {services.map((service, index) => (
              <RevealOnScroll key={service.serviceId} delay={(index % 3) * 80}>
                <ServiceCard service={service} />
              </RevealOnScroll>
            ))}
          </div>
        )}

        {!isLoading && !error && services.length === 0 && (
          <p className="mt-14 text-center text-sm text-mist-200/50">No services listed yet.</p>
        )}
      </div>
    </section>
  )
}
