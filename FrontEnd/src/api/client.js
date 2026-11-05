/**
 * Base da API. Vazio = mesma origem do front (em dev, o proxy do Vite repassa /api para a API).
 * Em produção, se a API estiver em outro endereço: VITE_API_BASE_URL=https://api.exemplo.com
 */
const BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

/** Erro de comunicação com a API, com mensagem pronta para exibir. */
export class ApiError extends Error {}

export async function getJson(path, { signal } = {}) {
  let response
  try {
    response = await fetch(`${BASE_URL}${path}`, { signal, headers: { Accept: 'application/json' } })
  } catch (error) {
    if (error.name === 'AbortError') throw new ApiError('Tempo de resposta da API esgotado')
    throw new ApiError('Sem conexão com a API')
  }

  if (!response.ok) {
    throw new ApiError(`API respondeu ${response.status}${response.statusText ? ` ${response.statusText}` : ''}`)
  }

  try {
    return await response.json()
  } catch {
    throw new ApiError('Resposta inválida da API')
  }
}
