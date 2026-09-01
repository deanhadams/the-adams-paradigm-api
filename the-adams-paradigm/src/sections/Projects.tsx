import { useState } from 'react'
import { ProjectCard } from '../components/ProjectCard'
import { ProjectModal } from '../components/ProjectModal'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'
import type { Project } from '../data/projects'
import { projects } from '../data/projects'

export function Projects() {
  const [activeProject, setActiveProject] = useState<Project | null>(null)

  return (
    <section id="projects" className="relative py-28 lg:py-36">
      <div className="mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading
            label="Built From Ideas"
            title="A selection of digital products and experiments."
            description="Each one started as a concept and was built out into a working, usable experience."
          />
        </RevealOnScroll>

        <div className="mt-14 grid gap-6 sm:grid-cols-2">
          {projects.map((project, index) => (
            <RevealOnScroll key={project.slug} delay={(index % 2) * 100}>
              <ProjectCard project={project} onOpen={setActiveProject} />
            </RevealOnScroll>
          ))}
        </div>
      </div>

      <ProjectModal project={activeProject} onClose={() => setActiveProject(null)} />
    </section>
  )
}
