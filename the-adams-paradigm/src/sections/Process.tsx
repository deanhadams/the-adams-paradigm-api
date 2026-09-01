import { cn } from '../lib/cn'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'
import { processSteps } from '../data/process'

export function Process() {
  return (
    <section id="process" className="relative py-28 lg:py-36">
      <div className="mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading label="How I Work" title="From Idea to Production" align="center" className="mx-auto" />
        </RevealOnScroll>

        <div className="relative mt-16">
          <div
            className="absolute left-[27px] top-2 bottom-2 w-px bg-gradient-to-b from-blue-electric via-emerald-glow to-transparent sm:left-1/2 sm:-translate-x-1/2"
            aria-hidden="true"
          />

          <ol className="space-y-10 sm:space-y-4">
            {processSteps.map((step, index) => {
              const isEven = index % 2 === 0
              return (
                <li key={step.index}>
                  <RevealOnScroll delay={40} className={cn('flex items-center gap-5 sm:gap-10', !isEven && 'sm:flex-row-reverse')}>
                    <div
                      className={cn(
                        'hidden sm:block sm:w-1/2',
                        isEven ? 'sm:text-right' : 'sm:text-left',
                      )}
                    >
                      <h3 className="text-xl font-bold text-mist-50">{step.title}</h3>
                      <p className="mt-2 text-sm leading-relaxed text-mist-200/65">{step.description}</p>
                    </div>

                    <div className="relative z-10 flex size-14 shrink-0 items-center justify-center rounded-2xl border border-emerald-glow/30 bg-navy-900 font-display text-lg font-bold text-emerald-glow shadow-[0_0_30px_-8px_rgba(52,211,153,0.5)]">
                      {step.index}
                    </div>

                    <div className="sm:hidden">
                      <h3 className="text-lg font-bold text-mist-50">{step.title}</h3>
                      <p className="mt-1.5 text-sm leading-relaxed text-mist-200/65">{step.description}</p>
                    </div>

                    <div className="hidden sm:block sm:w-1/2" />
                  </RevealOnScroll>
                </li>
              )
            })}
          </ol>
        </div>
      </div>
    </section>
  )
}
