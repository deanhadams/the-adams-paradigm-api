export interface ProcessStep {
  index: string
  title: string
  description: string
}

export const processSteps: ProcessStep[] = [
  { index: '01', title: 'Discover', description: 'Understand the idea, problem and desired outcome.' },
  { index: '02', title: 'Plan', description: 'Define functionality, architecture, technology and scope.' },
  { index: '03', title: 'Build', description: 'Develop the application, backend, integrations and UI.' },
  { index: '04', title: 'Test', description: 'Validate functionality, responsiveness and reliability.' },
  { index: '05', title: 'Launch', description: 'Deploy the application and make it available to users.' },
  { index: '06', title: 'Evolve', description: 'Improve, expand and maintain the product.' },
]
