import { Bot } from 'lucide-react'
import { cn } from '../lib/cn'
import type { ChatMessage as ChatMessageType } from '../hooks/useAiChat'

interface ChatMessageProps {
  message: ChatMessageType
}

function Avatar() {
  return (
    <div className="flex size-7 shrink-0 items-center justify-center rounded-full border border-white/10 bg-navy-900/60 text-emerald-glow">
      <Bot className="size-3.5" aria-hidden="true" />
    </div>
  )
}

export function ChatMessage({ message }: ChatMessageProps) {
  const isUser = message.role === 'user'

  return (
    <div className={cn('flex items-end gap-2', isUser ? 'justify-end' : 'justify-start')}>
      {!isUser && <Avatar />}
      <div
        className={cn(
          'max-w-[80%] whitespace-pre-wrap break-words rounded-2xl px-4 py-2.5 text-sm leading-relaxed',
          isUser
            ? 'rounded-br-sm border border-emerald-glow/20 bg-emerald-glow/[0.12] text-mist-50'
            : 'rounded-bl-sm border border-white/10 bg-white/[0.04] text-mist-100',
        )}
      >
        {message.content}
      </div>
    </div>
  )
}

export function TypingIndicator() {
  return (
    <div className="flex items-end gap-2">
      <Avatar />
      <div className="flex items-center gap-1 rounded-2xl rounded-bl-sm border border-white/10 bg-white/[0.04] px-4 py-3">
        <span className="size-1.5 animate-pulse-soft rounded-full bg-mist-200/60 [animation-delay:-0.3s]" />
        <span className="size-1.5 animate-pulse-soft rounded-full bg-mist-200/60 [animation-delay:-0.15s]" />
        <span className="size-1.5 animate-pulse-soft rounded-full bg-mist-200/60" />
      </div>
    </div>
  )
}
