import { AnimatedBackground } from '../components/AnimatedBackground'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionLabel } from '../components/SectionLabel'
import { principles } from '../data/philosophy'

export function Philosophy() {
  return (
    <section className="relative overflow-hidden bg-navy-900 py-28 lg:py-40">
      <AnimatedBackground />

      <div className="relative mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll className="text-center">
          <SectionLabel className="justify-center">Philosophy</SectionLabel>
          <h2 className="mx-auto mt-6 max-w-4xl text-4xl font-bold leading-[1.05] text-mist-50 sm:text-5xl lg:text-6xl">
            The <span className="text-gradient">Adams Paradigm</span>
          </h2>
          <p className="mx-auto mt-6 max-w-2xl text-base leading-relaxed text-mist-200/70 sm:text-lg">
            Technology should serve the idea — not the other way around.
          </p>
        </RevealOnScroll>

        <div className="mt-16 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {principles.map(({ icon: Icon, title, description }, index) => (
            <RevealOnScroll key={title} delay={index * 90}>
              <div className="group relative h-full rounded-2xl border border-white/10 bg-white/[0.03] p-7 transition-all duration-300 hover:-translate-y-1 hover:border-emerald-glow/30">
                <span className="text-4xl font-black text-white/5 transition-colors duration-300 group-hover:text-emerald-glow/10">
                  {String(index + 1).padStart(2, '0')}
                </span>
                <div className="-mt-6 flex size-12 items-center justify-center rounded-xl border border-white/10 bg-navy-950 text-emerald-glow">
                  <Icon className="size-5" aria-hidden="true" />
                </div>
                <h3 className="mt-5 text-lg font-semibold text-mist-50">{title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-mist-200/65">{description}</p>
              </div>
            </RevealOnScroll>
          ))}
        </div>
      </div>
    </section>
  )
}
