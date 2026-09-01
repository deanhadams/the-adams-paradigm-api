import { capabilityMarquee } from '../data/technologies'

const track = [...capabilityMarquee, ...capabilityMarquee]

export function CapabilityStrip() {
  return (
    <section className="relative border-y border-white/10 bg-navy-900/60 py-6" aria-label="Technologies Dean builds with">
      <div className="flex items-center gap-8 sm:gap-10">
        <span className="hidden shrink-0 pl-6 text-xs font-semibold uppercase tracking-[0.28em] text-mist-200/50 sm:block lg:pl-10">
          Building With
        </span>

        <div className="relative flex-1 overflow-hidden [mask-image:linear-gradient(to_right,transparent,black_8%,black_92%,transparent)]">
          <div className="animate-marquee flex w-max items-center gap-10 whitespace-nowrap">
            {track.map((item, index) => (
              <span
                key={`${item}-${index}`}
                className="flex items-center gap-10 text-sm font-medium text-mist-200/60"
              >
                {item}
                <span className="size-1 rounded-full bg-emerald-glow/50" aria-hidden="true" />
              </span>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
