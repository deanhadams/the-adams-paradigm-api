import { useEffect } from 'react'
import { CircleAlert, CircleCheck, CircleX, Clock, LoaderCircle } from 'lucide-react'
import { Button } from '../components/Button'
import { Footer } from '../components/Footer'
import { Navbar } from '../components/Navbar'
import { usePayfastStatus } from '../hooks/usePayfastStatus'

const PAGE_TITLE = 'Payment | The Adams Paradigm'

// Our order id travels in Payfast's return_url/cancel_url as ?orderId=... (set by the API).
// It only says which order to look up — never whether it was paid.
const orderId = new URLSearchParams(window.location.search).get('orderId')

interface PayfastReturnPageProps {
  // True on /payment/payfast-cancel: the customer cancelled on Payfast's page.
  cancelled?: boolean
}

export function PayfastReturnPage({ cancelled = false }: PayfastReturnPageProps) {
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
          {cancelled ? <CancelledState /> : <ReturnState />}

          {orderId && (
            <div className="rounded-xl border border-white/10 bg-navy-900/60 px-5 py-3">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">Order Number</p>
              <p className="mt-1 font-mono text-lg font-semibold text-mist-50">{orderId}</p>
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

function CancelledState() {
  return (
    <>
      <CircleX className="size-10 text-mist-200/70" aria-hidden="true" />
      <h1 className="font-display text-2xl font-bold text-mist-50">Payment cancelled</h1>
      <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
        You cancelled the payment and haven't been charged. Your booking isn't confirmed until it's paid — you can book
        again whenever you're ready.
      </p>
      <Button href="/#booking" variant="secondary" icon={false}>
        Book again
      </Button>
    </>
  )
}

function ReturnState() {
  const { state, result, error, checkAgain } = usePayfastStatus(orderId)

  if (state === 'checking') {
    return (
      <>
        <LoaderCircle className="size-10 animate-spin text-emerald-glow" aria-hidden="true" />
        <h1 className="font-display text-2xl font-bold text-mist-50">Confirming your payment…</h1>
        <p className="text-sm text-mist-200/70">This can take a few moments. Please don't close this page.</p>
      </>
    )
  }

  if (state === 'error') {
    return (
      <>
        <CircleAlert className="size-10 text-red-400" aria-hidden="true" />
        <h1 className="font-display text-2xl font-bold text-mist-50">We couldn't confirm your payment</h1>
        <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
          {error} If you were charged, you'll still receive a confirmation email once the payment is processed.
        </p>
      </>
    )
  }

  if (state === 'done' && result?.status === 'Paid') {
    return (
      <>
        <CircleCheck className="size-10 text-emerald-glow" aria-hidden="true" />
        <h1 className="font-display text-2xl font-bold text-mist-50">Payment successful</h1>
        <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
          Thank you! Your payment has been received and a confirmation email is on its way.
        </p>
      </>
    )
  }

  if (state === 'done' && result?.status === 'Cancelled') {
    return <CancelledState />
  }

  if (state === 'done') {
    return (
      <>
        <CircleAlert className="size-10 text-red-400" aria-hidden="true" />
        <h1 className="font-display text-2xl font-bold text-mist-50">Payment failed</h1>
        <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
          Your payment didn't go through and you have not been charged. Please try booking again.
        </p>
      </>
    )
  }

  return (
    <>
      <Clock className="size-10 text-blue-electric" aria-hidden="true" />
      <h1 className="font-display text-2xl font-bold text-mist-50">Payment pending</h1>
      <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
        We haven't received confirmation from Payfast yet. If you completed the payment, check again in a moment — you'll
        also get an email as soon as it's confirmed.
      </p>
      <Button onClick={checkAgain} variant="secondary" icon={false}>
        Check again
      </Button>
    </>
  )
}
