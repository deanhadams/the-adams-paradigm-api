import { useState } from 'react'
import { ClipboardList } from 'lucide-react'
import { BookingForm } from '../components/BookingForm'
import { OrdersModal } from '../components/OrdersModal'
import { RevealOnScroll } from '../components/RevealOnScroll'
import { SectionHeading } from '../components/SectionHeading'

export function Booking() {
  const [ordersOpen, setOrdersOpen] = useState(false)

  return (
    <section id="booking" className="relative py-28 lg:py-36">
      <div className="mx-auto w-[min(1280px,92vw)]">
        <RevealOnScroll>
          <SectionHeading
            align="center"
            label="Book a Service"
            title="Ready to Get Started?"
            description="Pick a service, add your details, and get a secure payment link — instantly."
            className="mx-auto"
          />
        </RevealOnScroll>

        <RevealOnScroll delay={80} className="mx-auto mt-8 flex justify-center">
          <button
            type="button"
            onClick={() => setOrdersOpen(true)}
            className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.03] px-5 py-2.5 text-sm font-semibold text-mist-50 backdrop-blur transition-all duration-300 hover:-translate-y-0.5 hover:border-white/30 hover:bg-white/[0.07]"
          >
            <ClipboardList className="size-4" aria-hidden="true" />
            View Bookings
          </button>
        </RevealOnScroll>

        <RevealOnScroll delay={120} className="mx-auto mt-12 max-w-2xl">
          <div className="glass rounded-3xl p-6 sm:p-10">
            <BookingForm />
          </div>
        </RevealOnScroll>
      </div>

      {ordersOpen && <OrdersModal onClose={() => setOrdersOpen(false)} />}
    </section>
  )
}
