/**
 * Self-hosted fonts for the Quant Terminal aesthetic (no CDN in prod).
 * Importing these registers the @font-face rules referenced by the
 * --qw-font-* tokens in tokens.css.
 *
 *   display / wordmark = Martian Mono  (tracked, all-caps labels)
 *   data / figures     = Spline Sans Mono (tabular)
 *   body / UI          = Hanken Grotesk
 */

// Martian Mono — display / wordmark
import '@fontsource/martian-mono/400.css'
import '@fontsource/martian-mono/500.css'
import '@fontsource/martian-mono/700.css'

// Spline Sans Mono — data / figures
import '@fontsource/spline-sans-mono/400.css'
import '@fontsource/spline-sans-mono/500.css'
import '@fontsource/spline-sans-mono/600.css'

// Hanken Grotesk — body / UI
import '@fontsource/hanken-grotesk/400.css'
import '@fontsource/hanken-grotesk/500.css'
import '@fontsource/hanken-grotesk/600.css'
import '@fontsource/hanken-grotesk/700.css'
