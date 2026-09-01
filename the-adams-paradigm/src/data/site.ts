export const site = {
  name: 'The Adams Paradigm',
  shortName: 'Adams Paradigm',
  founder: 'Dean Adams',
  tagline: 'Turning Ideas Into Powerful Digital Experiences.',
  description:
    'Dean Adams builds modern websites, web applications, APIs and digital products for small businesses, startups and individuals.',
  email: 'deanh.adams@gmail.com',
  availability: 'Available for new projects',
  socials: [] as { label: string; href: string }[],
} as const

export const navLinks = [
  { label: 'Home', href: '#home' },
  { label: 'Services', href: '#services' },
  { label: 'Booking', href: '#booking' },
  { label: 'Skills', href: '#skills' },
  { label: 'Projects', href: '#projects' },
  { label: 'About', href: '#about' },
  { label: 'Process', href: '#process' },
  { label: 'Contact', href: '#contact' },
] as const
