import { ArrowUpRight } from 'lucide-react'
import type { ElementType } from 'react'
import type { Project } from '../data/projects'
import { useMousePosition } from '../hooks/useMousePosition'
import { ProjectVisual } from './ProjectVisual'
import { TechBadge } from './TechBadge'

interface ProjectCardProps {
  project: Project
  onOpen: (project: Project) => void
}

export function ProjectCard({ project, onOpen }: ProjectCardProps) {
  const { ref, position, handleMouseMove } = useMousePosition<HTMLButtonElement & HTMLAnchorElement>()
  const isExternal = Boolean(project.url)

  const Wrapper = (isExternal ? 'a' : 'button') as ElementType
  const wrapperProps = isExternal
    ? { href: project.url, target: '_blank', rel: 'noopener noreferrer' }
    : { type: 'button', onClick: () => onOpen(project) }

  return (
    <Wrapper
      ref={ref}
      onMouseMove={handleMouseMove}
      className="group relative flex h-full flex-col overflow-hidden rounded-2xl border border-white/10 bg-white/[0.025] text-left transition-all duration-300 hover:-translate-y-1 hover:border-white/20"
      {...wrapperProps}
    >
      <ProjectVisual
        slug={project.slug}
        accent={project.accent}
        image={project.image}
        imageAlt={`${project.name} website preview`}
        className="h-52 w-full"
      >
        <div
          className="pointer-events-none absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
          style={{
            background: `radial-gradient(260px circle at ${position.x}% ${position.y}%, rgba(255,255,255,0.12), transparent 70%)`,
          }}
          aria-hidden="true"
        />
      </ProjectVisual>

      <div className="flex flex-1 flex-col p-6">
        <span className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-glow">{project.category}</span>
        <h3 className="mt-3 flex items-center gap-2 text-xl font-bold text-mist-50">
          {project.name}
          <ArrowUpRight
            className="size-4 -translate-y-0.5 text-mist-200/40 opacity-0 transition-all duration-300 group-hover:translate-x-0.5 group-hover:translate-y-0 group-hover:text-emerald-glow group-hover:opacity-100"
            aria-hidden="true"
          />
        </h3>
        <p className="mt-2 text-sm leading-relaxed text-mist-200/65">{project.description}</p>

        <div className="mt-5 flex flex-wrap gap-2">
          {project.tags.map((tag) => (
            <TechBadge key={tag} label={tag} />
          ))}
        </div>

        <span className="mt-6 inline-flex items-center gap-1.5 text-sm font-semibold text-mist-100 transition-colors group-hover:text-emerald-glow">
          {isExternal ? 'Visit Website' : 'View Project'}
          <ArrowUpRight className="size-3.5 transition-transform duration-300 group-hover:translate-x-0.5 group-hover:-translate-y-0.5" aria-hidden="true" />
        </span>
      </div>
    </Wrapper>
  )
}
