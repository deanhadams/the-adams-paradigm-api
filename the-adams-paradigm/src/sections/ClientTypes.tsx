import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'
import { clientTypes } from '../data/clientTypes'

export function ClientTypes() {
  return (
    <section className="relative overflow-hidden bg-mist-50 py-28 text-navy-950 lg:py-36">
      <div className="relative mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading light align="center" label="Who I Work With" title="Built For People With Ideas" className="mx-auto" />
        </RevealOnScroll>

        <div className="mt-14 grid gap-6 sm:grid-cols-3">
          {clientTypes.map(({ icon: Icon, title, description }, index) => (
            <RevealOnScroll key={title} delay={index * 100}>
              <div className="group relative h-full overflow-hidden rounded-2xl border border-navy-950/10 bg-white p-8 text-center shadow-[0_20px_45px_-30px_rgba(5,11,22,0.3)] transition-all duration-300 hover:-translate-y-1.5 hover:shadow-[0_30px_60px_-25px_rgba(5,11,22,0.35)]">
                <span className="absolute inset-x-0 top-0 h-1 origin-left scale-x-0 bg-gradient-to-r from-blue-electric to-emerald-glow transition-transform duration-500 group-hover:scale-x-100" />
                <div className="mx-auto flex size-14 items-center justify-center rounded-2xl bg-navy-950 text-emerald-glow">
                  <Icon className="size-6" aria-hidden="true" />
                </div>
                <h3 className="mt-5 text-lg font-bold text-navy-950">{title}</h3>
                <p className="mt-2 text-sm leading-relaxed text-navy-700">{description}</p>
              </div>
            </RevealOnScroll>
          ))}
        </div>
      </div>
    </section>
  )
}
