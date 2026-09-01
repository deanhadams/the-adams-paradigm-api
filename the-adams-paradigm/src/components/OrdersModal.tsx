import { type FormEvent, useEffect, useRef, useState } from 'react'
import { AlertCircle, ExternalLink, Loader2, Search, X } from 'lucide-react'
import { useOrdersByEmail } from '../hooks/useOrdersByEmail'
import { useLockBodyScroll } from '../hooks/useLockBodyScroll'
import { cn } from '../lib/cn'
import { inputClasses } from '../lib/formStyles'

interface OrdersModalProps {
  onClose: () => void
}

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function statusClasses(status: string): string {
  const normalized = status.toLowerCase()
  if (normalized === 'paid') return 'border-emerald-glow/30 bg-emerald-glow/[0.08] text-emerald-glow'
  if (normalized === 'failed' || normalized === 'cancelled') return 'border-red-400/30 bg-red-400/[0.08] text-red-400'
  return 'border-amber-400/30 bg-amber-400/[0.08] text-amber-300'
}

export function OrdersModal({ onClose }: OrdersModalProps) {
  useLockBodyScroll(true)
  const closeRef = useRef<HTMLButtonElement>(null)
  const [email, setEmail] = useState('')
  const [emailError, setEmailError] = useState<string | null>(null)
  const { status, orders, error, fetchOrders } = useOrdersByEmail()

  useEffect(() => {
    closeRef.current?.focus()

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const trimmed = email.trim()
    if (!trimmed) {
      setEmailError('Please enter your email.')
      return
    }
    if (!emailPattern.test(trimmed)) {
      setEmailError('Please enter a valid email address.')
      return
    }
    setEmailError(null)
    void fetchOrders(trimmed)
  }

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center overflow-y-auto p-4 py-10"
      role="dialog"
      aria-modal="true"
      aria-labelledby="orders-modal-title"
    >
      <div className="absolute inset-0 bg-navy-950/85 backdrop-blur-sm" onClick={onClose} />

      <div className="relative w-full max-w-2xl overflow-hidden rounded-2xl border border-white/10 bg-navy-900 shadow-2xl">
        <button
          ref={closeRef}
          type="button"
          onClick={onClose}
          aria-label="Close bookings lookup"
          className="absolute right-4 top-4 z-10 inline-flex size-9 items-center justify-center rounded-full border border-white/15 bg-navy-950/70 text-mist-100 backdrop-blur transition-colors hover:border-white/30 hover:text-emerald-glow"
        >
          <X className="size-4" aria-hidden="true" />
        </button>

        <div className="max-h-[80vh] overflow-y-auto p-8">
          <h3 id="orders-modal-title" className="text-2xl font-bold text-mist-50">
            View Your Bookings
          </h3>
          <p className="mt-2 text-sm leading-relaxed text-mist-200/70">
            Enter the email address you booked with to look up your orders.
          </p>

          <form noValidate onSubmit={handleSubmit} className="mt-6 flex flex-col gap-3 sm:flex-row">
            <div className="flex-1">
              <input
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => {
                  setEmail(e.target.value)
                  if (emailError) setEmailError(null)
                }}
                placeholder="you@example.com"
                aria-invalid={Boolean(emailError)}
                aria-describedby={emailError ? 'orders-email-error' : undefined}
                className={inputClasses(Boolean(emailError))}
              />
              {emailError && (
                <p id="orders-email-error" className="mt-1.5 text-xs font-medium text-red-400">
                  {emailError}
                </p>
              )}
            </div>

            <button
              type="submit"
              disabled={status === 'loading'}
              className="group inline-flex items-center justify-center gap-2 rounded-full bg-gradient-to-r from-blue-electric to-emerald-glow px-6 py-3.5 text-sm font-semibold text-navy-950 shadow-[0_18px_40px_-12px_rgba(59,130,246,0.55)] transition-all duration-300 hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {status === 'loading' ? (
                <Loader2 className="size-4 animate-spin" aria-hidden="true" />
              ) : (
                <Search className="size-4" aria-hidden="true" />
              )}
              Search
            </button>
          </form>

          {status === 'error' && (
            <p className="mt-4 flex items-center gap-2 text-sm font-medium text-red-400">
              <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
              Couldn't load your bookings. {error}
            </p>
          )}

          {status === 'success' && orders.length === 0 && (
            <p className="mt-6 rounded-xl border border-white/10 bg-navy-950/40 px-4 py-6 text-center text-sm text-mist-200/70">
              No bookings found for that email address.
            </p>
          )}

          {status === 'success' && orders.length > 0 && (
            <div className="mt-6 overflow-hidden rounded-xl border border-white/10">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead>
                    <tr className="border-b border-white/10 bg-navy-950/60 text-xs font-semibold uppercase tracking-[0.15em] text-mist-200/50">
                      <th className="px-4 py-3">Order Number</th>
                      <th className="px-4 py-3">Status</th>
                      <th className="px-4 py-3">Payment</th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders.map((order) => (
                      <tr key={order.orderNumber} className="border-b border-white/5 last:border-b-0 hover:bg-white/[0.02]">
                        <td className="px-4 py-3 font-mono text-xs text-mist-100/90">{order.orderNumber}</td>
                        <td className="px-4 py-3">
                          <span
                            className={cn(
                              'inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-semibold',
                              statusClasses(order.paymentStatus),
                            )}
                          >
                            {order.paymentStatus}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          {order.paymentLink ? (
                            <a
                              href={order.paymentLink}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center gap-1.5 text-sm font-semibold text-emerald-glow hover:underline"
                            >
                              View link
                              <ExternalLink className="size-3.5" aria-hidden="true" />
                            </a>
                          ) : (
                            <span className="text-sm text-mist-200/40">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
