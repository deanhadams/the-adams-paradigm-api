import { Braces, Database, Lightbulb, Plug, Sparkle } from 'lucide-react'

const nodes = [
  { label: 'Idea', icon: Lightbulb, top: '4%', left: '8%', delay: '0s' },
  { label: 'Code', icon: Braces, top: '26%', left: '58%', delay: '-2.5s' },
  { label: 'API', icon: Plug, top: '52%', left: '4%', delay: '-5s' },
  { label: 'Data', icon: Database, top: '70%', left: '52%', delay: '-7.5s' },
  { label: 'Experience', icon: Sparkle, top: '92%', left: '20%', delay: '-3.5s' },
]

const path = 'M 60 30 C 160 90, 220 130, 260 150 C 200 210, 120 240, 50 260 C 140 310, 220 340, 210 360'

export function SystemGraph() {
  return (
    <div className="relative mx-auto aspect-4/5 w-full max-w-md" aria-hidden="true">
      <svg viewBox="0 0 320 400" className="absolute inset-0 h-full w-full overflow-visible">
        <defs>
          <linearGradient id="graph-line" x1="0" y1="0" x2="320" y2="400" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#4d9fff" stopOpacity="0.7" />
            <stop offset="1" stopColor="#34d399" stopOpacity="0.7" />
          </linearGradient>
        </defs>
        <path d={path} fill="none" stroke="url(#graph-line)" strokeWidth="1.5" strokeDasharray="2 8" strokeLinecap="round" />
      </svg>

      {nodes.map(({ label, icon: Icon, top, left, delay }) => (
        <div
          key={label}
          className="animate-float-slow glass absolute flex -translate-x-1/2 -translate-y-1/2 flex-col items-center gap-1.5 rounded-2xl px-4 py-3 shadow-[0_20px_50px_-20px_rgba(0,0,0,0.6)]"
          style={{ top, left, animationDelay: delay }}
        >
          <Icon className="size-4 text-emerald-glow" />
          <span className="text-[0.65rem] font-semibold uppercase tracking-[0.18em] text-mist-100">{label}</span>
        </div>
      ))}
    </div>
  )
}
