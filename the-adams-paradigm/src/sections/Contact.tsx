import { useState } from 'react'
import { AnimatedBackground } from '../components/AnimatedBackground'
import { ContactForm } from '../components/ContactForm'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'
import { conversionPaths } from '../data/contact'
import { cn } from '../lib/cn'

export function Contact() {
  const [selectedPath, setSelectedPath] = useState(0)

  return (
    <section id="contact" className="relative overflow-hidden py-28 lg:py-36">
      <AnimatedBackground />

      <div className="relative mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading
            align="center"
            label="Let's Talk"
            title="Have An Idea? Let's Build It."
            description="Whether you have a fully defined project or just the beginning of an idea, start the conversation."
            className="mx-auto"
          />
        </RevealOnScroll>

        <RevealOnScroll delay={100}>
          <div
            role="radiogroup"
            aria-label="What would you like to do?"
            className="mx-auto mt-12 grid max-w-3xl gap-4 sm:grid-cols-3"
          >
            {conversionPaths.map((path, index) => {
              const isSelected = selectedPath === index
              return (
                <button
                  key={path.title}
                  type="button"
                  role="radio"
                  aria-checked={isSelected}
                  onClick={() => setSelectedPath(index)}
                  className={cn(
                    'rounded-2xl border p-5 text-left transition-all duration-300',
                    isSelected
                      ? 'border-emerald-glow/50 bg-emerald-glow/[0.08] shadow-[0_0_0_1px_rgba(52,211,153,0.2)]'
                      : 'border-white/10 bg-white/[0.02] hover:border-white/20',
                  )}
                >
                  <span className={cn('text-sm font-bold', isSelected ? 'text-emerald-glow' : 'text-mist-50')}>
                    {path.title}
                  </span>
                  <p className="mt-1.5 text-xs leading-relaxed text-mist-200/60">{path.description}</p>
                </button>
              )
            })}
          </div>
        </RevealOnScroll>

        <RevealOnScroll delay={180} className="mx-auto mt-10 max-w-2xl">
          <div className="glass rounded-3xl p-6 sm:p-10">
            <ContactForm contextLabel={conversionPaths[selectedPath].contextLabel} />
          </div>
        </RevealOnScroll>
      </div>
    </section>
  )
}
