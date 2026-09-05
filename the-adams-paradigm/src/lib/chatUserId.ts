const STORAGE_KEY = 'chat_user_id'

export function getChatUserId(): string {
  try {
    const existing = localStorage.getItem(STORAGE_KEY)
    if (existing) return existing

    const id = crypto.randomUUID()
    localStorage.setItem(STORAGE_KEY, id)
    return id
  } catch {
    return crypto.randomUUID()
  }
}
