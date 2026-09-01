import { Button } from '../components/Button'
import { ProjectVisual } from '../components/ProjectVisual'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionLabel } from '../components/SectionLabel'
import { TechBadge } from '../components/TechBadge'
import { featuredProject } from '../data/projects'

export function FeaturedProject() {
  const featured = featuredProject.featured!

  return (
    <section className="relative py-28 lg:py-36" aria-labelledby="featured-project-heading">
      <div className="mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionLabel>Featured Project</SectionLabel>
        </RevealOnScroll>

        <div className="mt-6 grid gap-12 overflow-hidden rounded-3xl border border-white/10 bg-white/[0.025] lg:grid-cols-2">
          <RevealOnScroll delay={80} className="relative min-h-[20rem]">
            <ProjectVisual
              slug={featuredProject.slug}
              accent={featuredProject.accent}
              image={featuredProject.image}
              imageAlt={`${featuredProject.name} website preview`}
              className="h-full w-full"
            />
          </RevealOnScroll>

          <RevealOnScroll delay={160} className="flex flex-col justify-center p-8 lg:p-12">
            <span className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-glow">
              {featuredProject.category}
            </span>
            <h3 id="featured-project-heading" className="mt-3 text-3xl font-bold text-mist-50 sm:text-4xl">
              {featuredProject.name}
            </h3>
            <p className="mt-4 text-base leading-relaxed text-mist-200/70">{featuredProject.description}</p>

            <div className="mt-6 space-y-4">
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">The Challenge</h4>
                <p className="mt-1.5 text-sm leading-relaxed text-mist-200/70">{featured.challenge}</p>
              </div>
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">The Solution</h4>
                <p className="mt-1.5 text-sm leading-relaxed text-mist-200/70">{featured.solution}</p>
              </div>
              <div>
                <h4 className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">Key Features</h4>
                <ul className="mt-2 grid gap-2 sm:grid-cols-2">
                  {featured.features.map((feature) => (
                    <li key={feature} className="flex items-center gap-2 text-sm text-mist-100/85">
                      <span className="size-1.5 shrink-0 rounded-full bg-emerald-glow" />
                      {feature}
                    </li>
                  ))}
                </ul>
              </div>
            </div>

            <div className="mt-6 flex flex-wrap gap-2">
              {featuredProject.tags.map((tag) => (
                <TechBadge key={tag} label={tag} />
              ))}
            </div>

            <Button href="#projects" className="mt-8 w-fit">
              Explore Project
            </Button>
          </RevealOnScroll>
        </div>
      </div>
    </section>
  )
}
