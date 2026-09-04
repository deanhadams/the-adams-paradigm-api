import { useCallback, useState } from 'react'
import { API_BASE_URL } from '../lib/config'

export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

const FALLBACK_ERROR_MESSAGE =
  "Sorry, I'm having trouble connecting right now. Please try again in a moment."

interface UseAiChatResult {
  messages: ChatMessage[]
  isSending: boolean
  sendMessage: (text: string) => Promise<void>
}

export function useAiChat(): UseAiChatResult {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [isSending, setIsSending] = useState(false)

  const sendMessage = useCallback(
    async (text: string) => {
      const trimmed = text.trim()

      if (!trimmed || isSending) return

      // Keep a copy of the conversation BEFORE adding
      // the new user message.
      const history = messages.map((message) => ({
        role: message.role,
        content: message.content,
      }))

      // Add the new user message to the UI immediately.
      setMessages((prev) => [
        ...prev,
        {
          role: 'user',
          content: trimmed,
        },
      ])

      setIsSending(true)

      try {
        const response = await fetch(`${API_BASE_URL}/api/ai/chat`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            message: trimmed,
            history,
          }),
        })

        if (!response.ok) {
          throw new Error(
            `Request failed with status ${response.status}`,
          )
        }

        const data: { answer?: string } = await response.json()

        const answer = data.answer?.trim()

        if (!answer) {
          throw new Error(
            'Received an empty response from the assistant.',
          )
        }

        setMessages((prev) => [
          ...prev,
          {
            role: 'assistant',
            content: answer,
          },
        ])
      } catch (err) {
        console.error('AI chat request failed:', err)

        setMessages((prev) => [
          ...prev,
          {
            role: 'assistant',
            content: FALLBACK_ERROR_MESSAGE,
          },
        ])
      } finally {
        setIsSending(false)
      }
    },
    [isSending, messages],
  )

  return {
    messages,
    isSending,
    sendMessage,
  }
}