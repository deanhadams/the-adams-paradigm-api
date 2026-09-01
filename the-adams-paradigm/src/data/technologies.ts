export interface TechGroup {
  id: string
  label: string
  description: string
  items: string[]
}

export const techGroups: TechGroup[] = [
  {
    id: 'frontend',
    label: 'Frontend',
    description: 'Interfaces that feel fast, responsive and intentional.',
    items: ['React', 'TypeScript', 'JavaScript', 'HTML', 'CSS', 'Tailwind CSS'],
  },
  {
    id: 'backend',
    label: 'Backend',
    description: 'Reliable systems that power the experience behind the scenes.',
    items: ['C#', '.NET', 'ASP.NET Core', 'Web APIs', 'SignalR'],
  },
  {
    id: 'data',
    label: 'Data',
    description: 'Structured, well-modeled data that scales with the product.',
    items: ['SQL Server', 'PostgreSQL', 'Relational Database Design'],
  },
  {
    id: 'integrations',
    label: 'Integrations',
    description: 'Connecting applications to the services they depend on.',
    items: ['REST APIs', 'Payment APIs', 'Webhooks', 'Authentication', 'Third-Party Services'],
  },
  {
    id: 'ai',
    label: 'AI',
    description: 'Practical intelligence layered into real product features.',
    items: ['AI APIs', 'Generative AI', 'AI-Powered Application Features'],
  },
  {
    id: 'deployment',
    label: 'Deployment',
    description: 'Getting code from a repository into a live, working product.',
    items: ['Git', 'GitHub', 'Cloud Hosting', 'Production Deployment'],
  },
]

export const capabilityMarquee = [
  'C#',
  '.NET',
  'ASP.NET Core',
  'React',
  'TypeScript',
  'SQL',
  'APIs',
  'AI',
  'Cloud',
]
