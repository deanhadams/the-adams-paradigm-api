import { useEffect, useRef } from 'react'
import { X } from 'lucide-react'
import type { Project } from '../data/projects'
import { useLockBodyScroll } from '../hooks/useLockBodyScroll'
import { ProjectVisual } from './ProjectVisual'
import { TechBadge } from './TechBadge'

interface ProjectModalProps {
  project: Project | null
  onClose: () => void
}

export function ProjectModal({ project, onClose }: ProjectModalProps) {
  useLockBodyScroll(Boolean(project))
  const closeRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!project) return

    closeRef.current?.focus()

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [project, onClose])

  if (!project) return null

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center overflow-y-auto p-4 py-10"
      role="dialog"
      aria-modal="true"
      aria-labelledby="project-modal-title"
    >
      <div className="absolute inset-0 bg-navy-950/85 backdrop-blur-sm" onClick={onClose} />

      <div className="relative w-full max-w-2xl overflow-hidden rounded-2xl border border-white/10 bg-navy-900 shadow-2xl">
        <button
          ref={closeRef}
          type="button"
          onClick={onClose}
          aria-label="Close project details"
          className="absolute right-4 top-4 z-10 inline-flex size-9 items-center justify-center rounded-full border border-white/15 bg-navy-950/70 text-mist-100 backdrop-blur transition-colors hover:border-white/30 hover:text-emerald-glow"
        >
          <X className="size-4" aria-hidden="true" />
        </button>

        <ProjectVisual
          slug={project.slug}
          accent={project.accent}
          image={project.image}
          imageAlt={`${project.name} website preview`}
          className="h-56 w-full"
        />

        <div className="max-h-[60vh] overflow-y-auto p-8">
          <span className="text-xs font-semibold uppercase tracking-[0.2em] text-emerald-glow">{project.category}</span>
          <h3 id="project-modal-title" className="mt-2 text-2xl font-bold text-mist-50">
            {project.name}
          </h3>
          <p className="mt-4 text-sm leading-relaxed text-mist-200/70">{project.description}</p>

          <div className="mt-6">
            <h4 className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">Highlights</h4>
            <ul className="mt-3 grid gap-2 sm:grid-cols-2">
              {project.highlights.map((highlight) => (
                <li key={highlight} className="flex items-center gap-2 text-sm text-mist-100/85">
                  <span className="size-1.5 shrink-0 rounded-full bg-emerald-glow" />
                  {highlight}
                </li>
              ))}
            </ul>
          </div>

          <div className="mt-6 flex flex-wrap gap-2">
            {project.tags.map((tag) => (
              <TechBadge key={tag} label={tag} />
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
