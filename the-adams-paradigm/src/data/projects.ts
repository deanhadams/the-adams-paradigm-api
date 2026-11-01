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
    url: 'https://lineup-legends.vercel.app/',
    image: 'https://pub-26d0794de3654ed6a3b6ada1126ee4b0.r2.dev/projects/lineup-legends.png',
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
    slug: 'jellyb-rally',
    name: 'JellyB Rally',
    category: 'Volleyball • Arcade Game • Browser Game',
    description:
      'A browser-based 2D volleyball game starring giant jelly bean characters, playable with touch or mouse in an upright-phone layout. Score a point whenever the ball touches your opponent\'s ground.',
    // TODO: tech stack unconfirmed — replace with the actual stack (engine/framework) once known.
    highlights: ['2D game physics', 'Touch & mouse controls', 'Upright mobile layout', 'Score-based gameplay'],
    tags: ['Game', 'Browser', '2D'],
    accent: 'blue',
    url: 'https://jelly-b-rally.vercel.app/',
    image: 'https://pub-26d0794de3654ed6a3b6ada1126ee4b0.r2.dev/projects/jellyb-rally.png',
  },
  {
    slug: 'world-quiz-league',
    name: 'World Quiz League',
    category: 'Quiz Platform • Real-Time Experience',
    description:
      'An interactive quiz competition platform — "Where Knowledge Competes" — built around categories, difficulty, scoring, competition and leaderboards.',
    highlights: ['Real-time interaction', 'Game mechanics', 'Scoring', 'Leaderboards', 'Question systems'],
    tags: ['SignalR', 'Real-Time', 'Leaderboards'],
    accent: 'blue',
    url: 'https://world-quiz-league-598313888394.europe-west2.run.app/',
    image: '/projects/WorldQuizLeague.png',
  },
  {
    slug: 'isyncal',
    name: 'iSyncal',
    category: 'Developer Tool • REST API • SaaS Product',
    description:
      'A REST API service that lets developers read and write iCloud Calendar events without touching the CalDAV protocol directly.',
    highlights: ['REST API', 'CalDAV Integration', 'Apple App-Password Auth', 'Recurring Events (RRULE)', 'Calendar Sync'],
    tags: ['REST API', 'CalDAV', 'iCloud Calendar', 'Developer Tool'],
    accent: 'emerald',
    url: 'https://icloud-calendar-dashboard-production.up.railway.app/',
    image: '/projects/iSynCal.png',
  },
]

export const featuredProject = projects.find((project) => project.slug === 'complete-the-eleven-fc')!
