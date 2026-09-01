import { navLinks, site } from '../data/site'
import { Logo } from './Logo'

const footerLinks = navLinks.filter((link) =>
  ['Home', 'Services', 'Projects', 'About', 'Contact'].includes(link.label),
)

export function Footer() {
  return (
    <footer className="relative border-t border-white/10 bg-navy-950">
      <div className="mx-auto max-w-7xl px-6 py-16 lg:px-10">
        <div className="grid gap-12 lg:grid-cols-[1.4fr_1fr_1fr]">
          <div>
            <Logo />
            <p className="mt-5 max-w-sm text-sm leading-relaxed text-mist-200/60">
              {site.founder} — “{site.tagline}”
            </p>
          </div>

          <div>
            <h3 className="text-xs font-semibold uppercase tracking-[0.24em] text-mist-200/50">Navigate</h3>
            <ul className="mt-5 space-y-3">
              {footerLinks.map((link) => (
                <li key={link.href}>
                  <a href={link.href} className="text-sm text-mist-200/70 transition-colors hover:text-emerald-glow">
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className="text-xs font-semibold uppercase tracking-[0.24em] text-mist-200/50">Get In Touch</h3>
            <ul className="mt-5 space-y-3">
              <li>
                <a
                  href={`mailto:${site.email}`}
                  className="text-sm text-mist-200/70 transition-colors hover:text-emerald-glow"
                >
                  {site.email}
                </a>
              </li>
              <li className="text-sm text-mist-200/50">Built with React + TypeScript</li>
            </ul>
          </div>
        </div>

        <div className="mt-14 flex flex-col gap-4 border-t border-white/5 pt-8 text-xs text-mist-200/40 sm:flex-row sm:items-center sm:justify-between">
          <p>© 2026 {site.name}. All rights reserved.</p>
          <p className="font-medium uppercase tracking-[0.2em]">{site.tagline}</p>
        </div>
      </div>
    </footer>
  )
}
