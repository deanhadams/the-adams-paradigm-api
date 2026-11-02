import { useEffect } from 'react'
import { CircleAlert, CircleCheck, Clock, LoaderCircle } from 'lucide-react'
import { Button } from '../components/Button'
import { Footer } from '../components/Footer'
import { Navbar } from '../components/Navbar'
import { usePaystackVerification } from '../hooks/usePaystackVerification'

const PAGE_TITLE = 'Payment | The Adams Paradigm'

// Paystack redirects here after checkout with ?reference=...&trxref=... (both carry the
// same value). The page never decides the outcome itself — it asks the API, which
// verifies the transaction with Paystack server-side.
function readReference(): string | null {
  const params = new URLSearchParams(window.location.search)
  return params.get('reference') ?? params.get('trxref')
}

const reference = readReference()

export function PaystackCallbackPage() {
  const { status, result, error, verify } = usePaystackVerification(reference)

  useEffect(() => {
    const previousTitle = document.title
    document.title = PAGE_TITLE
    window.scrollTo(0, 0)

    return () => {
      document.title = previousTitle
    }
  }, [])

  return (
    <>
      <Navbar />
      <main className="relative px-6 pt-36 pb-24 lg:px-10 lg:pt-44">
        <div className="mx-auto flex max-w-xl flex-col items-center gap-5 rounded-2xl border border-white/10 bg-white/[0.03] px-6 py-12 text-center backdrop-blur">
          {status === 'verifying' && (
            <>
              <LoaderCircle className="size-10 animate-spin text-emerald-glow" aria-hidden="true" />
              <h1 className="font-display text-2xl font-bold text-mist-50">Confirming your payment…</h1>
              <p className="text-sm text-mist-200/70">This only takes a moment. Please don't close this page.</p>
            </>
          )}

          {status === 'error' && (
            <>
              <CircleAlert className="size-10 text-red-400" aria-hidden="true" />
              <h1 className="font-display text-2xl font-bold text-mist-50">We couldn't confirm your payment</h1>
              <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
                {error} If you were charged, you'll still receive a confirmation email once the payment is processed.
              </p>
              {reference && (
                <Button onClick={verify} variant="secondary" icon={false}>
                  Try again
                </Button>
              )}
            </>
          )}

          {status === 'done' && result?.status === 'Paid' && (
            <>
              <CircleCheck className="size-10 text-emerald-glow" aria-hidden="true" />
              <h1 className="font-display text-2xl font-bold text-mist-50">Payment successful</h1>
              <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
                Thank you! Your payment has been received and a confirmation email is on its way.
              </p>
            </>
          )}

          {status === 'done' && result?.status === 'Failed' && (
            <>
              <CircleAlert className="size-10 text-red-400" aria-hidden="true" />
              <h1 className="font-display text-2xl font-bold text-mist-50">Payment failed</h1>
              <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
                Your payment didn't go through and you have not been charged. Please try booking again.
              </p>
            </>
          )}

          {status === 'done' && result && result.status !== 'Paid' && result.status !== 'Failed' && (
            <>
              <Clock className="size-10 text-blue-electric" aria-hidden="true" />
              <h1 className="font-display text-2xl font-bold text-mist-50">Payment pending</h1>
              <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
                We haven't received confirmation from Paystack yet. If you completed the payment, check again in a
                moment — you'll also get an email as soon as it's confirmed.
              </p>
              <Button onClick={verify} variant="secondary" icon={false}>
                Check again
              </Button>
            </>
          )}

          {result && (
            <div className="rounded-xl border border-white/10 bg-navy-900/60 px-5 py-3">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">Order Number</p>
              <p className="mt-1 font-mono text-lg font-semibold text-mist-50">{result.orderNumber}</p>
            </div>
          )}

          <Button href="/" variant="ghost" icon={false}>
            Back to home
          </Button>
        </div>
      </main>
      <Footer />
    </>
  )
}
