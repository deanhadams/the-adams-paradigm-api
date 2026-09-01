import { AnimatedBackground } from '../components/AnimatedBackground'
import { Button } from '../components/Button'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { StatusIndicator } from '../components/StatusIndicator'
import { SystemGraph } from '../components/SystemGraph'
import { site } from '../data/site'
import { useMousePosition } from '../hooks/useMousePosition'

export function Hero() {
  const { ref, position, handleMouseMove } = useMousePosition<HTMLDivElement>()

  return (
    <section
      id="home"
      ref={ref}
      onMouseMove={handleMouseMove}
      className="relative flex min-h-screen items-center overflow-hidden pt-32 pb-20"
    >
      <AnimatedBackground variant="hero" />

      <div
        className="pointer-events-none absolute inset-0 opacity-70 transition-opacity duration-500"
        style={{
          background: `radial-gradient(600px circle at ${position.x}% ${position.y}%, rgba(77,159,255,0.09), transparent 60%)`,
        }}
        aria-hidden="true"
      />

      <div className="relative mx-auto grid w-[min(1280px,92vw)] items-center gap-16 lg:grid-cols-[1.15fr_0.85fr] lg:gap-8">
        <div>
          <RevealOnScroll>
            <span className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.3em] text-emerald-glow">
              Dean Adams <span className="text-mist-200/30">•</span> Full-Stack Developer
            </span>
          </RevealOnScroll>

          <RevealOnScroll delay={80}>
            <h1 className="mt-6 text-4xl font-bold leading-[1.06] text-mist-50 sm:text-5xl lg:text-6xl xl:text-7xl">
              Turning Ideas Into <span className="text-gradient">Powerful Digital Experiences.</span>
            </h1>
          </RevealOnScroll>

          <RevealOnScroll delay={160}>
            <p className="mt-7 max-w-xl text-base leading-relaxed text-mist-200/70 sm:text-lg">
              I build modern web applications, APIs, integrations and business platforms — taking an idea from a
              rough concept through development, testing and deployment into a real, working product.
            </p>
          </RevealOnScroll>

          <RevealOnScroll delay={240}>
            <div className="mt-9 flex flex-col gap-4 sm:flex-row sm:items-center">
              <Button href="#contact">Start a Project</Button>
              <Button href="#projects" variant="secondary">
                Explore My Work
              </Button>
            </div>
          </RevealOnScroll>

          <RevealOnScroll delay={320}>
            <div className="mt-10">
              <StatusIndicator label={site.availability} />
            </div>
          </RevealOnScroll>
        </div>

        <RevealOnScroll delay={200} className="hidden lg:block">
          <SystemGraph />
        </RevealOnScroll>
      </div>
    </section>
  )
}
