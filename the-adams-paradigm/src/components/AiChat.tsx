import { useEffect, useRef, useState } from 'react'
import { Bot, X } from 'lucide-react'
import { useAiChat } from '../hooks/useAiChat'
import { cn } from '../lib/cn'
import { ChatInput } from './ChatInput'
import { ChatMessage, TypingIndicator } from './ChatMessage'
import { StatusIndicator } from './StatusIndicator'

const SUGGESTED_QUESTIONS = [
  'What services do you offer?',
  'How much do your services cost?',
  'Who is Dean Adams?',
  'Can you build an AI application?',
  'Tell me about your projects.',
  'How do I book a service?',
]

export function AiChat() {
  const [isOpen, setIsOpen] = useState(false)
  const { messages, isSending, sendMessage } = useAiChat()
  const scrollAnchorRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!isOpen) return
    scrollAnchorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [messages, isSending, isOpen])

  useEffect(() => {
    if (!isOpen) return
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsOpen(false)
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [isOpen])

  return (
    <>
      <div
        role="dialog"
        aria-modal="false"
        aria-label="The Adams Paradigm AI assistant"
        aria-hidden={!isOpen}
        className={cn(
          'fixed inset-4 z-50 flex flex-col overflow-hidden rounded-2xl border border-white/10 bg-navy-900/95 shadow-2xl backdrop-blur-xl transition-all duration-300 sm:inset-auto sm:bottom-24 sm:right-6 sm:h-[600px] sm:max-h-[calc(100vh-140px)] sm:w-[400px]',
          isOpen
            ? 'pointer-events-auto translate-y-0 scale-100 opacity-100'
            : 'pointer-events-none translate-y-4 scale-95 opacity-0',
        )}
      >
        <div className="flex shrink-0 items-center justify-between gap-3 border-b border-white/10 bg-navy-950/60 px-5 py-4">
          <div>
            <h3 className="font-display text-base font-semibold text-mist-50">The Adams Paradigm AI</h3>
            <p className="mt-0.5 text-xs text-mist-200/60">Ask me about Dean, services, projects &amp; more</p>
          </div>
          <div className="flex shrink-0 items-center gap-2">
            <StatusIndicator label="Online" className="px-2.5 py-1.5 text-[11px]" />
            <button
              type="button"
              onClick={() => setIsOpen(false)}
              aria-label="Close AI assistant"
              className="inline-flex size-8 items-center justify-center rounded-full border border-white/10 text-mist-200/70 transition-colors hover:border-white/25 hover:text-emerald-glow"
            >
              <X className="size-4" aria-hidden="true" />
            </button>
          </div>
        </div>

        <div className="flex-1 space-y-4 overflow-y-auto px-4 py-4">
          <div className="flex items-end gap-2">
            <div className="flex size-7 shrink-0 items-center justify-center rounded-full border border-white/10 bg-navy-900/60 text-emerald-glow">
              <Bot className="size-3.5" aria-hidden="true" />
            </div>
            <div className="max-w-[85%] space-y-2 rounded-2xl rounded-bl-sm border border-white/10 bg-white/[0.04] px-4 py-3 text-sm leading-relaxed text-mist-100">
              <p>Hi! I'm the Adams Paradigm AI assistant.</p>
              <p>
                I can answer questions about Dean Adams, services, projects, technologies, pricing, bookings and
                more.
              </p>
              <p>What would you like to know?</p>
            </div>
          </div>

          {messages.length === 0 && (
            <div className="flex flex-wrap gap-2 pl-9">
              {SUGGESTED_QUESTIONS.map((question) => (
                <button
                  key={question}
                  type="button"
                  onClick={() => sendMessage(question)}
                  className="rounded-full border border-white/10 bg-white/[0.02] px-3 py-1.5 text-xs font-medium text-mist-200/80 transition-colors hover:border-emerald-glow/40 hover:text-emerald-glow"
                >
                  {question}
                </button>
              ))}
            </div>
          )}

          {messages.map((message, index) => (
            <ChatMessage key={index} message={message} />
          ))}

          {isSending && <TypingIndicator />}

          <div ref={scrollAnchorRef} />
        </div>

        <ChatInput disabled={isSending} onSend={sendMessage} />
      </div>

      <button
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-label={isOpen ? 'Close AI assistant' : 'Open AI assistant'}
        className="fixed bottom-6 right-6 z-50 inline-flex size-14 items-center justify-center rounded-full bg-gradient-to-r from-blue-electric to-emerald-glow text-navy-950 shadow-[0_18px_40px_-12px_rgba(59,130,246,0.55)] transition-all duration-300 hover:-translate-y-0.5 hover:shadow-[0_22px_50px_-10px_rgba(52,211,153,0.6)]"
      >
        {isOpen ? <X className="size-6" aria-hidden="true" /> : <Bot className="size-6" aria-hidden="true" />}
      </button>
    </>
  )
}
