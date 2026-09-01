import { cn } from './cn'

export function inputClasses(hasError: boolean): string {
  return cn(
    'w-full rounded-xl border bg-navy-900/60 px-4 py-3 text-sm text-mist-50 placeholder:text-mist-200/35 outline-none transition-colors duration-200',
    hasError ? 'border-red-400/60 focus:border-red-400' : 'border-white/10 focus:border-emerald-glow/50',
  )
}
