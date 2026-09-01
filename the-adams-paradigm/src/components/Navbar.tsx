import { useEffect, useState } from 'react'
import { Menu, X } from 'lucide-react'
import { navLinks, site } from '../data/site'
import { useScrollSpy } from '../hooks/useScrollSpy'
import { cn } from '../lib/cn'
import { Button } from './Button'
import { Logo } from './Logo'
import { MobileMenu } from './MobileMenu'

const sectionIds = navLinks.map((link) => link.href.replace('#', ''))

export function Navbar() {
  const [scrolled, setScrolled] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const activeId = useScrollSpy(sectionIds)

  useEffect(() => {
    const handleScroll = () => setScrolled(window.scrollY > 24)
    handleScroll()
    window.addEventListener('scroll', handleScroll, { passive: true })
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  return (
    <>
      <header
        className={cn(
          'fixed inset-x-0 top-0 z-50 flex justify-center transition-all duration-500',
          scrolled ? 'pt-3' : 'pt-5',
        )}
      >
        <nav
          className={cn(
            'flex w-[min(1180px,92vw)] items-center justify-between rounded-2xl border border-white/10 bg-navy-950/70 px-5 backdrop-blur-xl transition-all duration-500',
            scrolled ? 'py-2.5 shadow-[0_10px_40px_-15px_rgba(0,0,0,0.6)]' : 'py-3.5',
          )}
          aria-label="Primary"
        >
          <a href="#home" className="group shrink-0" aria-label={`${site.name} — home`}>
            <Logo />
          </a>

          <ul className="hidden items-center gap-1 lg:flex">
            {navLinks.map((link) => {
              const isActive = activeId === link.href.replace('#', '')
              return (
                <li key={link.href}>
                  <a
                    href={link.href}
                    aria-current={isActive ? 'true' : undefined}
                    className={cn(
                      'relative rounded-full px-4 py-2 text-sm font-medium transition-colors duration-200',
                      isActive ? 'text-mist-50' : 'text-mist-200/60 hover:text-mist-50',
                    )}
                  >
                    {link.label}
                    {isActive && (
                      <span className="absolute inset-x-3 -bottom-0.5 h-px bg-gradient-to-r from-blue-electric to-emerald-glow" />
                    )}
                  </a>
                </li>
              )
            })}
          </ul>

          <div className="hidden lg:block">
            <Button href="#contact" variant="primary" className="px-5 py-2.5 text-sm">
              Start a Project
            </Button>
          </div>

          <button
            type="button"
            className="inline-flex items-center justify-center rounded-full border border-white/10 p-2.5 text-mist-100 lg:hidden"
            aria-label={mobileOpen ? 'Close menu' : 'Open menu'}
            aria-expanded={mobileOpen}
            onClick={() => setMobileOpen((open) => !open)}
          >
            {mobileOpen ? <X className="size-5" aria-hidden="true" /> : <Menu className="size-5" aria-hidden="true" />}
          </button>
        </nav>
      </header>

      <MobileMenu open={mobileOpen} activeId={activeId} onClose={() => setMobileOpen(false)} />
    </>
  )
}
