import { Code2, Puzzle, Sparkles, Wand2 } from 'lucide-react'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'

const focusAreas = [
  { icon: Puzzle, label: 'Problem solving' },
  { icon: Code2, label: 'Connecting frontend and backend systems' },
  { icon: Wand2, label: 'Integrating APIs' },
  { icon: Sparkles, label: 'Creating polished user experiences' },
]

export function About() {
  return (
    <section id="about" className="relative overflow-hidden bg-mist-50 py-28 text-navy-950 lg:py-36">
      <div className="bg-grid absolute inset-0 opacity-[0.4] [mask-image:radial-gradient(ellipse_70%_60%_at_50%_0%,black,transparent)]" aria-hidden="true" />

      <div className="relative mx-auto grid w-[min(1280px,92vw)] gap-14 lg:grid-cols-[1fr_1fr] lg:gap-20">
        <RevealOnScroll>
          <SectionHeading light label="About" title="Meet Dean" />
          <div className="mt-6 space-y-5 text-base leading-relaxed text-navy-700 sm:text-lg">
            <p>
              I'm a developer who enjoys taking ideas from rough concepts and turning them into functioning digital
              products. I like the full range of the process — planning how something should work, building the
              interface, wiring up the backend, and getting it in front of real users.
            </p>
            <p>
              My focus is on building practical solutions: applications that connect a well-designed frontend to a
              solid backend, integrate cleanly with the APIs and services they depend on, and hold up in production —
              not just in a demo.
            </p>
            <p>
              I'm always learning new technologies as they become useful, and I care about the details that make an
              application feel good to use, not just function correctly.
            </p>
          </div>
        </RevealOnScroll>

        <RevealOnScroll delay={120}>
          <div className="grid gap-4 sm:grid-cols-2">
            {focusAreas.map(({ icon: Icon, label }) => (
              <div
                key={label}
                className="rounded-2xl border border-navy-950/10 bg-white p-6 shadow-[0_20px_45px_-25px_rgba(5,11,22,0.25)] transition-transform duration-300 hover:-translate-y-1"
              >
                <div className="flex size-11 items-center justify-center rounded-xl bg-navy-950 text-emerald-glow">
                  <Icon className="size-5" aria-hidden="true" />
                </div>
                <p className="mt-4 text-sm font-semibold leading-snug text-navy-900">{label}</p>
              </div>
            ))}
          </div>
        </RevealOnScroll>
      </div>
    </section>
  )
}
