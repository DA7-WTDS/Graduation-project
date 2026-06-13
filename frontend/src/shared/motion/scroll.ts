import { useReducedMotion, useTransform, type MotionValue } from 'motion/react'

export { useScroll, useTransform } from 'motion/react'

/**
 * Map a 0..1 scroll progress to a parallax offset (px), disabled under
 * `prefers-reduced-motion` (returns a constant 0). Use for the hero RiskSurface
 * drift and similar scroll-driven movement.
 */
export function useParallax(progress: MotionValue<number>, distance: number): MotionValue<number> {
    const shouldReduce = useReducedMotion()
    return useTransform(progress, [0, 1], [0, shouldReduce ? 0 : distance])
}
