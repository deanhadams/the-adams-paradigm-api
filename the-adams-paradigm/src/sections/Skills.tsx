import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'
import { TechBadge } from '../components/TechBadge'
import { techGroups } from '../data/technologies'

export function Skills() {
  return (
    <section id="skills" className="relative overflow-hidden py-28 lg:py-36">
      <div
        className="pointer-events-none absolute inset-0 opacity-60 [mask-image:radial-gradient(ellipse_70%_60%_at_50%_40%,black,transparent)]"
        aria-hidden="true"
      >
        <div className="bg-grid absolute inset-0 opacity-30" />
      </div>

      <div className="relative mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading
            label="Technology"
            title="The Technology Behind the Work"
            description="A practical, connected toolkit — from interface to database to deployment."
          />
        </RevealOnScroll>

        <div className="mt-14 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {techGroups.map((group, index) => (
            <RevealOnScroll key={group.id} delay={(index % 3) * 90}>
              <div className="group relative h-full overflow-hidden rounded-2xl border border-white/10 bg-white/[0.025] p-6 transition-all duration-300 hover:-translate-y-1 hover:border-emerald-glow/30">
                <div
                  className="pointer-events-none absolute -right-10 -top-10 size-40 rounded-full bg-blue-electric/10 blur-3xl transition-opacity duration-300 group-hover:opacity-100 opacity-0"
                  aria-hidden="true"
                />
                <span className="text-[0.65rem] font-bold uppercase tracking-[0.28em] text-emerald-glow">
                  {String(index + 1).padStart(2, '0')}
                </span>
                <h3 className="mt-3 text-lg font-semibold text-mist-50">{group.label}</h3>
                <p className="mt-2 text-sm leading-relaxed text-mist-200/60">{group.description}</p>
                <div className="mt-5 flex flex-wrap gap-2">
                  {group.items.map((item) => (
                    <TechBadge key={item} label={item} />
                  ))}
                </div>
              </div>
            </RevealOnScroll>
          ))}
        </div>
      </div>
    </section>
  )
}
