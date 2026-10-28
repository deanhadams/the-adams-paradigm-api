import { useEffect } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import termsMarkdown from '../content/terms-and-conditions.md?raw'
import { Footer } from '../components/Footer'
import { Navbar } from '../components/Navbar'

const PAGE_TITLE = 'Terms and Conditions | The Adams Paradigm'
const PAGE_DESCRIPTION =
  'The Terms and Conditions for theadamsparadigm.com — bookings, payments, cancellations, intellectual property and more.'

function setMetaDescription(content: string) {
  let tag = document.querySelector('meta[name="description"]')
  if (!tag) {
    tag = document.createElement('meta')
    tag.setAttribute('name', 'description')
    document.head.appendChild(tag)
  }
  tag.setAttribute('content', content)
}

// Strip the leading "# Terms and Conditions – The Adams Paradigm" heading from the
// markdown body — it's rendered separately as the page's own <h1> below, so the
// document isn't left with two top-level headings.
const termsBody = termsMarkdown.replace(/^#\s+.+\n+/, '')

export function TermsPage() {
  useEffect(() => {
    const previousTitle = document.title
    document.title = PAGE_TITLE
    setMetaDescription(PAGE_DESCRIPTION)
    window.scrollTo(0, 0)

    return () => {
      document.title = previousTitle
    }
  }, [])

  return (
    <>
      <Navbar />
      <main className="relative px-6 pt-36 pb-24 lg:px-10 lg:pt-44">
        <div className="mx-auto max-w-3xl">
          <h1 className="font-display text-3xl font-bold leading-[1.1] text-mist-50 sm:text-4xl">
            Terms and Conditions
          </h1>
          <p className="mt-3 text-sm text-mist-200/50">The Adams Paradigm</p>

          <div
            className="prose prose-invert mt-10 max-w-none
              prose-headings:font-display prose-headings:font-bold prose-headings:tracking-tight prose-headings:text-mist-50
              prose-h2:mt-12 prose-h2:text-xl prose-h2:sm:text-2xl
              prose-p:leading-relaxed prose-p:text-mist-200/80
              prose-li:text-mist-200/80 prose-li:marker:text-emerald-glow
              prose-a:text-emerald-glow prose-a:no-underline hover:prose-a:underline
              prose-strong:text-mist-50
              prose-em:text-mist-200/60"
          >
            <ReactMarkdown remarkPlugins={[remarkGfm]}>{termsBody}</ReactMarkdown>
          </div>
        </div>
      </main>
      <Footer />
    </>
  )
}
