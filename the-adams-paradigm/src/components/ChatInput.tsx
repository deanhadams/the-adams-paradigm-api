import { type KeyboardEvent, useRef, useState } from 'react'
import { Send } from 'lucide-react'
import { cn } from '../lib/cn'

interface ChatInputProps {
  disabled: boolean
  onSend: (message: string) => void
}

const MAX_HEIGHT_PX = 120

export function ChatInput({ disabled, onSend }: ChatInputProps) {
  const [value, setValue] = useState('')
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  const resize = () => {
    const el = textareaRef.current
    if (!el) return
    el.style.height = 'auto'
    el.style.height = `${Math.min(el.scrollHeight, MAX_HEIGHT_PX)}px`
  }

  const submit = () => {
    const trimmed = value.trim()
    if (!trimmed || disabled) return
    onSend(trimmed)
    setValue('')
    requestAnimationFrame(resize)
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      submit()
    }
  }

  return (
    <div className="flex items-end gap-2 border-t border-white/10 bg-navy-950/40 p-3">
      <textarea
        ref={textareaRef}
        value={value}
        onChange={(e) => {
          setValue(e.target.value)
          resize()
        }}
        onKeyDown={handleKeyDown}
        disabled={disabled}
        rows={1}
        placeholder="Ask me anything..."
        aria-label="Message"
        className="max-h-[120px] flex-1 resize-none rounded-xl border border-white/10 bg-navy-900/60 px-3.5 py-2.5 text-sm text-mist-50 placeholder:text-mist-200/35 outline-none transition-colors duration-200 focus:border-emerald-glow/50 disabled:cursor-not-allowed disabled:opacity-60"
      />
      <button
        type="button"
        onClick={submit}
        disabled={disabled || !value.trim()}
        aria-label="Send message"
        className={cn(
          'inline-flex size-10 shrink-0 items-center justify-center rounded-full bg-gradient-to-r from-blue-electric to-emerald-glow text-navy-950 shadow-[0_10px_30px_-10px_rgba(52,211,153,0.6)] transition-all duration-300',
          disabled || !value.trim() ? 'cursor-not-allowed opacity-50' : 'hover:-translate-y-0.5',
        )}
      >
        <Send className="size-4" aria-hidden="true" />
      </button>
    </div>
  )
}
