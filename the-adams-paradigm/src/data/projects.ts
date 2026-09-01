export interface Project {
  slug: string
  name: string
  category: string
  description: string
  highlights: string[]
  tags: string[]
  accent: string
  url?: string
  image?: string
  featured?: {
    challenge: string
    solution: string
    features: string[]
  }
}

export const projects: Project[] = [
  {
    slug: 'complete-the-eleven-fc',
    name: 'Complete The Eleven FC',
    category: 'Football • Interactive Game • Web Application',
    description:
      'An interactive football game built around legendary starting lineups where players must identify the missing star.',
    highlights: ['Game logic', 'Interactive UI', 'Football data', 'Responsive design', 'User engagement'],
    tags: ['React', 'TypeScript', 'Game Logic', 'Responsive UI'],
    accent: 'emerald',
    featured: {
      challenge:
        'Turn static football trivia into a genuinely replayable game — one that tests recall against real starting lineups without feeling like a quiz form.',
      solution:
        'A lineup-based guessing engine that presents legendary starting elevens with one player missing, backed by a responsive board UI that works as well on a phone as it does on a desktop.',
      features: [
        'Lineup and squad data modeling',
        'Round-based guessing logic with scoring',
        'Responsive pitch-style layout',
        'Fast, reactive UI built in React',
      ],
    },
  },
  {
    slug: 'devil-bunny',
    name: 'Devil Bunny',
    category: 'Anime • Digital Universe • Creative Platform',
    description: 'A large-scale original anime/manga universe developed into a digital experience.',
    highlights: ['World building', 'Character systems', 'Storytelling', 'Digital presentation', 'Creative technology'],
    tags: ['World Building', 'Content Systems', 'Digital Presentation'],
    accent: 'blue',
    url: 'https://devil-bunny-official-series.ai.studio/',
    image: 'https://pub-26d0794de3654ed6a3b6ada1126ee4b0.r2.dev/projects/devil-bunny-site.png',
  },
  {
    slug: 'world-quiz-league',
    name: 'World Quiz League',
    category: 'Quiz Platform • Real-Time Experience',
    description:
      'An interactive quiz platform concept designed around categories, difficulty, scoring, competition and leaderboards.',
    highlights: ['Real-time interaction', 'Game mechanics', 'Scoring', 'Leaderboards', 'Question systems'],
    tags: ['SignalR', 'Real-Time', 'Leaderboards'],
    accent: 'blue',
  },
  {
    slug: 'flowdesk',
    name: 'FlowDesk',
    category: 'Professional Product Concept',
    description:
      'A modern business operations platform bringing bookings, customer management, payments and workflow automation into one place.',
    highlights: ['Authentication', 'Dashboard', 'Booking', 'Payments', 'Database', 'API architecture', 'Notifications'],
    tags: ['ASP.NET Core', 'React', 'SQL Server', 'Payments API'],
    accent: 'emerald',
  },
]

export const featuredProject = projects.find((project) => project.slug === 'complete-the-eleven-fc')!
