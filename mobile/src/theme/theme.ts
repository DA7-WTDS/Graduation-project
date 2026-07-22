/**
 * The "Quant Terminal" design language, ported from the web app's CSS tokens
 * to React Native. Dark ink surfaces, amber accent, mono figures.
 */
export const colors = {
    ink: '#0B0E11',
    panel: '#12161B',
    panel2: '#171C22',

    text: '#E8EAED',
    textDim: '#8A9099',
    textFaint: '#767D87',

    amber: '#FFB000',
    amberDim: '#B57A00',
    amberGlow: 'rgba(255, 176, 0, 0.14)',

    border: 'rgba(255, 255, 255, 0.08)',
    borderStrong: 'rgba(255, 255, 255, 0.14)',
    grid: 'rgba(255, 255, 255, 0.04)',

    // Signal / P&L semantics (shared vocab with the pipeline).
    buy: '#3DDC84',
    sell: '#FF5247',
    hold: '#FFB000',
    low: '#3DDC84',
    med: '#FFB000',
    high: '#FF5247',
} as const

export const radius = {
    sm: 8,
    md: 12,
    lg: 16,
    pill: 999,
} as const

export const spacing = {
    xs: 4,
    sm: 8,
    md: 12,
    lg: 16,
    xl: 24,
    xxl: 32,
} as const

// React Native ships a monospace family per platform; the Quant Terminal look
// leans on mono figures. System sans for body until we bundle the brand fonts.
export const fonts = {
    body: undefined as string | undefined, // platform default sans
    mono: 'monospace',
} as const

export const fontSize = {
    xs: 11,
    sm: 13,
    base: 15,
    h4: 19,
    fig: 21,
    h3: 22,
    h2: 27,
    h1: 32,
    hero: 42,
} as const

/** Map a risk level / signal string to its semantic color. */
export function signalColor(v?: string | null): string {
    switch ((v ?? '').toUpperCase()) {
        case 'BUY':
        case 'POSITIVE':
        case 'LOW':
            return colors.buy
        case 'SELL':
        case 'NEGATIVE':
        case 'HIGH':
            return colors.sell
        default:
            return colors.hold
    }
}
