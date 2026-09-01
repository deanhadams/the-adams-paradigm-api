import { type FormEvent, useMemo, useState } from 'react'
import { AlertCircle, CalendarCheck, ExternalLink, Loader2, X } from 'lucide-react'
import { useCreateCheckout } from '../hooks/useCreateCheckout'
import { useServices } from '../hooks/useServices'
import { inputClasses } from '../lib/formStyles'
import { FormField } from './FormField'

interface FormState {
  serviceId: string
  name: string
  surname: string
  email: string
}

const initialState: FormState = {
  serviceId: '',
  name: '',
  surname: '',
  email: '',
}

type Errors = Partial<Record<keyof FormState, string>>

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function validate(values: FormState): Errors {
  const errors: Errors = {}
  if (!values.serviceId) errors.serviceId = 'Please select a service.'
  if (!values.name.trim()) errors.name = 'Please enter your name.'
  if (!values.surname.trim()) errors.surname = 'Please enter your surname.'
  if (!values.email.trim()) {
    errors.email = 'Please enter your email.'
  } else if (!emailPattern.test(values.email.trim())) {
    errors.email = 'Please enter a valid email address.'
  }
  return errors
}

const currencyFormatter = new Intl.NumberFormat('en-ZA', {
  style: 'currency',
  currency: 'ZAR',
  maximumFractionDigits: 2,
})

export function BookingForm() {
  const { services, isLoading: servicesLoading, error: servicesError, refetch: refetchServices } = useServices()
  const { status, result, error, createCheckout, reset } = useCreateCheckout()

  const [values, setValues] = useState<FormState>(initialState)
  const [errors, setErrors] = useState<Errors>({})

  const selectedService = useMemo(
    () => services.find((service) => service.serviceId === Number(values.serviceId)) ?? null,
    [services, values.serviceId],
  )

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setValues((prev) => ({ ...prev, [key]: value }))
    if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }))
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0 || !selectedService) return

    void createCheckout({
      serviceId: selectedService.serviceId,
      name: values.name.trim(),
      surname: values.surname.trim(),
      email: values.email.trim(),
      amount: selectedService.costPerHour,
    })
  }

  if (status === 'success' && result) {
    return (
      <div
        className="relative flex flex-col items-center gap-4 rounded-2xl border border-emerald-glow/30 bg-emerald-glow/[0.06] px-8 py-14 text-center"
        role="status"
      >
        <button
          type="button"
          onClick={() => {
            setValues(initialState)
            reset()
          }}
          aria-label="Close"
          className="absolute right-4 top-4 inline-flex size-8 items-center justify-center rounded-full border border-white/15 bg-navy-950/40 text-mist-200/70 transition-colors hover:border-white/30 hover:text-emerald-glow"
        >
          <X className="size-4" aria-hidden="true" />
        </button>

        <CalendarCheck className="size-10 text-emerald-glow" aria-hidden="true" />
        <h3 className="text-xl font-bold text-mist-50">Booking Created</h3>

        <div className="rounded-xl border border-white/10 bg-navy-900/60 px-5 py-3">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-mist-200/50">Order Number</p>
          <p className="mt-1 font-mono text-lg font-semibold text-mist-50">{result.orderId}</p>
        </div>

        <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
          Amount due: {currencyFormatter.format(result.amount)} {result.currency}. A confirmation email with these
          details has been sent to {values.email}.
        </p>

        {result.paymentUrl && (
          <a
            href={result.paymentUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="group inline-flex items-center gap-2 rounded-full bg-gradient-to-r from-blue-electric to-emerald-glow px-6 py-3.5 text-sm font-semibold text-navy-950 shadow-[0_18px_40px_-12px_rgba(59,130,246,0.55)] transition-all duration-300 hover:-translate-y-0.5"
          >
            Complete Payment
            <ExternalLink className="size-4 transition-transform duration-300 group-hover:translate-x-0.5 group-hover:-translate-y-0.5" aria-hidden="true" />
          </a>
        )}

        <button
          type="button"
          onClick={() => {
            setValues(initialState)
            reset()
          }}
          className="mt-2 text-sm font-semibold text-emerald-glow hover:underline"
        >
          Book another service
        </button>
      </div>
    )
  }

  return (
    <form noValidate onSubmit={handleSubmit} className="space-y-5">
      <FormField label="Service" htmlFor="serviceId" error={errors.serviceId}>
        <select
          id="serviceId"
          name="serviceId"
          value={values.serviceId}
          onChange={(e) => setField('serviceId', e.target.value)}
          disabled={servicesLoading || Boolean(servicesError)}
          aria-invalid={Boolean(errors.serviceId)}
          aria-describedby={errors.serviceId ? 'serviceId-error' : undefined}
          className={inputClasses(Boolean(errors.serviceId))}
        >
          <option value="" disabled>
            {servicesLoading ? 'Loading services…' : 'Select a service'}
          </option>
          {services.map((service) => (
            <option key={service.serviceId} value={service.serviceId}>
              {service.title} — {currencyFormatter.format(service.costPerHour)}/hr
            </option>
          ))}
        </select>
      </FormField>

      {servicesError && (
        <p className="flex items-center gap-2 text-xs font-medium text-red-400">
          <AlertCircle className="size-3.5 shrink-0" aria-hidden="true" />
          Couldn't load services. {servicesError}
          <button type="button" onClick={refetchServices} className="font-semibold text-emerald-glow hover:underline">
            Try again
          </button>
        </p>
      )}

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField label="Name" htmlFor="name" error={errors.name}>
          <input
            id="name"
            name="name"
            type="text"
            autoComplete="given-name"
            value={values.name}
            onChange={(e) => setField('name', e.target.value)}
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? 'name-error' : undefined}
            className={inputClasses(Boolean(errors.name))}
            placeholder="Your first name"
          />
        </FormField>

        <FormField label="Surname" htmlFor="surname" error={errors.surname}>
          <input
            id="surname"
            name="surname"
            type="text"
            autoComplete="family-name"
            value={values.surname}
            onChange={(e) => setField('surname', e.target.value)}
            aria-invalid={Boolean(errors.surname)}
            aria-describedby={errors.surname ? 'surname-error' : undefined}
            className={inputClasses(Boolean(errors.surname))}
            placeholder="Your surname"
          />
        </FormField>
      </div>

      <FormField label="Email" htmlFor="email" error={errors.email}>
        <input
          id="email"
          name="email"
          type="email"
          autoComplete="email"
          value={values.email}
          onChange={(e) => setField('email', e.target.value)}
          aria-invalid={Boolean(errors.email)}
          aria-describedby={errors.email ? 'email-error' : undefined}
          className={inputClasses(Boolean(errors.email))}
          placeholder="you@example.com"
        />
      </FormField>

      {selectedService && (
        <div className="flex items-center justify-between rounded-xl border border-white/10 bg-navy-900/60 px-4 py-3">
          <span className="text-sm text-mist-200/70">Amount due</span>
          <span className="text-lg font-semibold text-mist-50">
            {currencyFormatter.format(selectedService.costPerHour)}
          </span>
        </div>
      )}

      {status === 'error' && (
        <p className="flex items-center gap-2 text-sm font-medium text-red-400">
          <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
          Couldn't create your booking. {error}
        </p>
      )}

      <button
        type="submit"
        disabled={status === 'submitting' || servicesLoading}
        className="group inline-flex w-full items-center justify-center gap-2 rounded-full bg-gradient-to-r from-blue-electric to-emerald-glow px-6 py-3.5 text-sm font-semibold text-navy-950 shadow-[0_18px_40px_-12px_rgba(59,130,246,0.55)] transition-all duration-300 hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-70 sm:w-auto"
      >
        {status === 'submitting' ? (
          <>
            <Loader2 className="size-4 animate-spin" aria-hidden="true" />
            Creating booking…
          </>
        ) : (
          <>
            Book &amp; Pay
            <CalendarCheck className="size-4" aria-hidden="true" />
          </>
        )}
      </button>
    </form>
  )
}
