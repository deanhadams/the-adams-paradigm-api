export const projectTypeOptions = [
  'Website',
  'Web Application',
  'Custom Software',
  'API / Integration',
  'Payment System',
  'Booking System',
  'AI Application',
  'Other',
] as const

export const budgetOptions = [
  'Not sure yet',
  'Under R5,000',
  'R5,000 – R15,000',
  'R15,000 – R50,000',
  'R50,000+',
] as const

export interface ConversionPath {
  title: string
  description: string
  contextLabel: string
}

export const conversionPaths: ConversionPath[] = [
  {
    title: 'Start a Project',
    description: 'For people ready to discuss development.',
    contextLabel: 'Starting a project',
  },
  {
    title: 'Request a Quote',
    description: 'For people who know what they need.',
    contextLabel: 'Requesting a quote',
  },
  {
    title: 'Book a Consultation',
    description: 'For people who want to discuss an idea first.',
    contextLabel: 'Booking a consultation',
  },
]
