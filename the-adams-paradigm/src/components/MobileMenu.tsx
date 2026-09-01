import { navLinks } from '../data/site'
import { useLockBodyScroll } from '../hooks/useLockBodyScroll'
import { cn } from '../lib/cn'
import { Button } from './Button'

interface MobileMenuProps {
  open: boolean
  activeId: string
  onClose: () => void
}

export function MobileMenu({ open, activeId, onClose }: MobileMenuProps) {
  useLockBodyScroll(open)

  return (
    <div
      className={cn(
        'fixed inset-0 z-40 lg:hidden',
        open ? 'pointer-events-auto' : 'pointer-events-none',
      )}
      aria-hidden={!open}
    >
      <div
        className={cn(
          'absolute inset-0 bg-navy-950/80 backdrop-blur-sm transition-opacity duration-300',
          open ? 'opacity-100' : 'opacity-0',
        )}
        onClick={onClose}
      />

      <div
        className={cn(
          'absolute inset-x-4 top-24 origin-top rounded-2xl border border-white/10 bg-navy-900/95 p-6 shadow-2xl transition-all duration-300',
          open ? 'translate-y-0 scale-100 opacity-100' : '-translate-y-4 scale-95 opacity-0',
        )}
      >
        <ul className="flex flex-col divide-y divide-white/5">
          {navLinks.map((link) => {
            const isActive = activeId === link.href.replace('#', '')
            return (
              <li key={link.href}>
                <a
                  href={link.href}
                  onClick={onClose}
                  aria-current={isActive ? 'true' : undefined}
                  className={cn(
                    'block py-3.5 text-lg font-semibold transition-colors',
                    isActive ? 'text-emerald-glow' : 'text-mist-50',
                  )}
                >
                  {link.label}
                </a>
              </li>
            )
          })}
        </ul>
        <Button href="#contact" onClick={onClose} className="mt-6 w-full justify-center">
          Start a Project
        </Button>
      </div>
    </div>
  )
}
