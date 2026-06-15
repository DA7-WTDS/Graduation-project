/**
 * Hardened API client for QuantWise.
 *
 * - Typed `apiCall<T>` wrapper around fetch.
 * - Central 401 interceptor: clears the token and redirects to /login
 *   (once, guarding against redirect loops on the auth pages).
 * - Normalized error shape via `ApiError` (preserves HTTP status so callers
 *   can branch on 404 = "no resource yet" vs. real failures).
 *
 * All backend routes are under `/api/*`.
 */

export const API_BASE_URL: string =
    (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5000'

const TOKEN_KEY = 'token'

export const getToken = (): string | null => localStorage.getItem(TOKEN_KEY)
export const setToken = (token: string): void => localStorage.setItem(TOKEN_KEY, token)
export const removeToken = (): void => localStorage.removeItem(TOKEN_KEY)

/** Normalized error surfaced to callers. `status` is the HTTP status (0 = network error). */
export class ApiError extends Error {
    readonly status: number
    readonly body: unknown

    constructor(message: string, status: number, body: unknown = null) {
        super(message)
        this.name = 'ApiError'
        this.status = status
        this.body = body
    }
}

export interface ApiCallOptions extends Omit<RequestInit, 'body'> {
    /** Attach the bearer token from localStorage. */
    requireAuth?: boolean
    /** Pre-serialized request body (callers JSON.stringify themselves, as today). */
    body?: BodyInit | null
}

function buildHeaders(requireAuth: boolean, extra?: HeadersInit): Headers {
    const headers = new Headers(extra)
    if (!headers.has('Content-Type')) {
        headers.set('Content-Type', 'application/json')
    }
    if (requireAuth) {
        const token = getToken()
        if (token) {
            headers.set('Authorization', `Bearer ${token}`)
        }
    }
    return headers
}

// Routes that never force a redirect on 401 (the token is still cleared).
// Includes the public landing/auth pages so a stale token doesn't bounce a
// logged-out visitor off them when AuthContext auto-loads the profile on mount.
const PUBLIC_PATHS = ['/', '/login', '/signup']

/** On expiry/invalid token: clear it and bounce to /login (unless on a public page). */
function handleUnauthorized(): void {
    removeToken()
    const path = window.location.pathname
    if (!PUBLIC_PATHS.includes(path)) {
        window.location.assign('/login')
    }
}

export async function apiCall<T = unknown>(
    endpoint: string,
    options: ApiCallOptions = {},
): Promise<T> {
    const { requireAuth = false, headers, ...rest } = options
    const url = `${API_BASE_URL}${endpoint}`

    let response: Response
    try {
        response = await fetch(url, {
            ...rest,
            headers: buildHeaders(requireAuth, headers),
        })
    } catch (err) {
        // Network / CORS / server-down — no HTTP status available.
        throw new ApiError(
            err instanceof Error ? err.message : 'Network request failed',
            0,
        )
    }

    const data: unknown = await response.json().catch(() => null)

    if (!response.ok) {
        if (response.status === 401) {
            handleUnauthorized()
        }

        const problem = data as { detail?: string; title?: string; message?: string } | null
        const message =
            problem?.detail ||
            problem?.title ||
            problem?.message ||
            `HTTP ${response.status}: ${response.statusText}`

        throw new ApiError(message, response.status, data)
    }

    return data as T
}
