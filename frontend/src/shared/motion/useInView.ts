import { useRef } from 'react'
import { useInView as useMotionInView } from 'motion/react'

type InViewOptions = Parameters<typeof useMotionInView>[1]

/**
 * Convenience wrapper around Framer Motion's `useInView`: returns a ref to attach
 * and a boolean. Defaults to firing once when the element is ~15% into view —
 * the standard for the homepage section reveals.
 */
export function useInView(options?: InViewOptions) {
    const ref = useRef<HTMLDivElement>(null)
    const inView = useMotionInView(ref, { once: true, margin: '-15%', ...options })
    return { ref, inView }
}
