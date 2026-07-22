import { getItem, setItem, deleteItem } from './storage'

/**
 * Native API client for the QuantWise mobile app. Mirrors the web client's
 * contract (typed apiCall, ApiError with HTTP status) but reads the bearer token
 * from secure storage and has no window/redirect coupling — 401 handling is the
 * navigator's job, surfaced via the ApiError status.
 *
 * The base URL is configured per-platform: the Android emulator reaches the host
 * machine at 10.0.2.2, a real device needs the host LAN IP, and web/iOS-sim use
 * localhost. Override with EXPO_PUBLIC_API_URL.
 */
import { Platform } from 'react-native'

function defaultBaseUrl(): string {
    const fromEnv = process.env.EXPO_PUBLIC_API_URL
    if (fromEnv) {
        return fromEnv
    }
    // Local dev backend runs on 5099 (WSL reserved 5000).
    if (Platform.OS === 'android') {
        return 'http://10.0.2.2:5099'
    }
    return 'http://localhost:5099'
}

export const API_BASE_URL = defaultBaseUrl()

const TOKEN_KEY = 'qw_token'

export const getToken = () => getItem(TOKEN_KEY)
export const setToken = (token: string) => setItem(TOKEN_KEY, token)
export const removeToken = () => deleteItem(TOKEN_KEY)

/** Normalized error with the HTTP status (0 = network error) so callers can
 * branch on 404 = "no resource yet" vs. a real failure. */
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
    requireAuth?: boolean
    body?: BodyInit | null
}

async function buildHeaders(requireAuth: boolean, extra?: HeadersInit): Promise<Headers> {
    const headers = new Headers(extra)
    if (!headers.has('Content-Type')) {
        headers.set('Content-Type', 'application/json')
    }
    if (requireAuth) {
        const token = await getToken()
        if (token) {
            headers.set('Authorization', `Bearer ${token}`)
        }
    }
    return headers
}

export async function apiCall<T = unknown>(
    endpoint: string,
    options: ApiCallOptions = {},
): Promise<T> {
    const { requireAuth = false, headers, ...rest } = options
    const url = `${API_BASE_URL}${endpoint}`

    let response: Response
    try {
        response = await fetch(url, { ...rest, headers: await buildHeaders(requireAuth, headers) })
    } catch (err) {
        throw new ApiError(err instanceof Error ? err.message : 'Network request failed', 0)
    }

    const data: unknown = await response.json().catch(() => null)

    if (!response.ok) {
        if (response.status === 401) {
            await removeToken()
        }
        const problem = data as { detail?: string; title?: string; message?: string } | null
        const message =
            problem?.detail || problem?.title || problem?.message ||
            `HTTP ${response.status}: ${response.statusText}`
        throw new ApiError(message, response.status, data)
    }

    return data as T
}
