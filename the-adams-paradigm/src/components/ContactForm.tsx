import { type FormEvent, useState } from 'react'
import { CheckCircle2, Loader2, Send } from 'lucide-react'
import { budgetOptions, projectTypeOptions } from '../data/contact'
import { site } from '../data/site'
import { cn } from '../lib/cn'
import { inputClasses } from '../lib/formStyles'
import { FormField } from './FormField'

interface FormState {
  name: string
  email: string
  projectType: string
  budget: string
  message: string
}

const initialState: FormState = {
  name: '',
  email: '',
  projectType: '',
  budget: '',
  message: '',
}

type Errors = Partial<Record<keyof FormState, string>>
type Status = 'idle' | 'submitting' | 'success' | 'error'

interface ContactFormProps {
  contextLabel?: string
}

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function validate(values: FormState): Errors {
  const errors: Errors = {}
  if (!values.name.trim()) errors.name = 'Please enter your name.'
  if (!values.email.trim()) {
    errors.email = 'Please enter your email.'
  } else if (!emailPattern.test(values.email.trim())) {
    errors.email = 'Please enter a valid email address.'
  }
  if (!values.projectType) errors.projectType = 'Please select a project type.'
  if (!values.message.trim()) {
    errors.message = 'Please add a short description of your project.'
  } else if (values.message.trim().length < 20) {
    errors.message = 'Please provide a few more details (at least 20 characters).'
  }
  return errors
}

export function ContactForm({ contextLabel }: ContactFormProps) {
  const [values, setValues] = useState<FormState>(initialState)
  const [errors, setErrors] = useState<Errors>({})
  const [status, setStatus] = useState<Status>('idle')

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setValues((prev) => ({ ...prev, [key]: value }))
    if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }))
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const validationErrors = validate(values)
    setErrors(validationErrors)

    if (Object.keys(validationErrors).length > 0) {
      setStatus('idle')
      return
    }

    setStatus('submitting')

    const subject = `${contextLabel ?? 'New project inquiry'} — ${values.name}`
    const body = [
      `Name: ${values.name}`,
      `Email: ${values.email}`,
      `Project type: ${values.projectType}`,
      `Budget: ${values.budget || 'Not specified'}`,
      '',
      values.message,
    ].join('\n')

    const mailtoUrl = `mailto:${site.email}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`

    window.setTimeout(() => {
      window.location.href = mailtoUrl
      setStatus('success')
    }, 500)
  }

  if (status === 'success') {
    return (
      <div className="flex flex-col items-center gap-4 rounded-2xl border border-emerald-glow/30 bg-emerald-glow/[0.06] px-8 py-14 text-center" role="status">
        <CheckCircle2 className="size-10 text-emerald-glow" aria-hidden="true" />
        <h3 className="text-xl font-bold text-mist-50">Your email client is opening</h3>
        <p className="max-w-sm text-sm leading-relaxed text-mist-200/70">
          A message addressed to {site.email} has been prepared with your project details. Send it from your email
          app to complete your inquiry.
        </p>
        <button
          type="button"
          onClick={() => {
            setValues(initialState)
            setStatus('idle')
          }}
          className="mt-2 text-sm font-semibold text-emerald-glow hover:underline"
        >
          Send another message
        </button>
      </div>
    )
  }

  return (
    <form noValidate onSubmit={handleSubmit} className="space-y-5">
      <div className="grid gap-5 sm:grid-cols-2">
        <FormField label="Name" htmlFor="name" error={errors.name}>
          <input
            id="name"
            name="name"
            type="text"
            autoComplete="name"
            value={values.name}
            onChange={(e) => setField('name', e.target.value)}
            aria-invalid={Boolean(errors.name)}
            aria-describedby={errors.name ? 'name-error' : undefined}
            className={inputClasses(Boolean(errors.name))}
            placeholder="Your name"
          />
        </FormField>

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
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField label="Project Type" htmlFor="projectType" error={errors.projectType}>
          <select
            id="projectType"
            name="projectType"
            value={values.projectType}
            onChange={(e) => setField('projectType', e.target.value)}
            aria-invalid={Boolean(errors.projectType)}
            aria-describedby={errors.projectType ? 'projectType-error' : undefined}
            className={inputClasses(Boolean(errors.projectType))}
          >
            <option value="" disabled>
              Select a project type
            </option>
            {projectTypeOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </FormField>

        <FormField label="Budget Range" htmlFor="budget" error={errors.budget}>
          <select
            id="budget"
            name="budget"
            value={values.budget}
            onChange={(e) => setField('budget', e.target.value)}
            className={inputClasses(false)}
          >
            <option value="" disabled>
              Select a budget range
            </option>
            {budgetOptions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </FormField>
      </div>

      <FormField label="Message" htmlFor="message" error={errors.message}>
        <textarea
          id="message"
          name="message"
          rows={5}
          value={values.message}
          onChange={(e) => setField('message', e.target.value)}
          aria-invalid={Boolean(errors.message)}
          aria-describedby={errors.message ? 'message-error' : undefined}
          className={cn(inputClasses(Boolean(errors.message)), 'resize-none')}
          placeholder="Tell me a little about your idea or project..."
        />
      </FormField>

      <p className="text-xs text-mist-200/45">
        Budget ranges are shown in South African Rand (ZAR) and are a starting point for conversation, not a fixed
        quote.
      </p>

      <button
        type="submit"
        disabled={status === 'submitting'}
        className="group inline-flex w-full items-center justify-center gap-2 rounded-full bg-gradient-to-r from-blue-electric to-emerald-glow px-6 py-3.5 text-sm font-semibold text-navy-950 shadow-[0_18px_40px_-12px_rgba(59,130,246,0.55)] transition-all duration-300 hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-70 sm:w-auto"
      >
        {status === 'submitting' ? (
          <>
            <Loader2 className="size-4 animate-spin" aria-hidden="true" />
            Preparing message...
          </>
        ) : (
          <>
            Send Message
            <Send className="size-4 transition-transform duration-300 group-hover:translate-x-1" aria-hidden="true" />
          </>
        )}
      </button>
    </form>
  )
}
