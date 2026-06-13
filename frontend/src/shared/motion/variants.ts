/**
 * Centralized Framer Motion variants so every page composes the same
 * orchestrated-load primitives. Components gate these behind
 * `useReducedMotion()` (from 'motion/react') where appropriate.
 */
import type { Variants } from 'motion/react'

/** Simple opacity fade. */
export const fadeIn: Variants = {
    hidden: { opacity: 0 },
    show: { opacity: 1, transition: { duration: 0.4, ease: 'easeOut' } },
}

/** Fade + rise — the default reveal for cards/sections. */
export const fadeInUp: Variants = {
    hidden: { opacity: 0, y: 16 },
    show: { opacity: 1, y: 0, transition: { duration: 0.45, ease: [0.22, 1, 0.36, 1] } },
}

/** Subtle scale-in for tiles/figures. */
export const scaleIn: Variants = {
    hidden: { opacity: 0, scale: 0.96 },
    show: { opacity: 1, scale: 1, transition: { duration: 0.35, ease: 'easeOut' } },
}

/** Parent that staggers its children's entrance. */
export const staggerContainer: Variants = {
    hidden: {},
    show: { transition: { staggerChildren: 0.06, delayChildren: 0.04 } },
}

/** Child used inside `staggerContainer` (one dense row / pick). */
export const staggerItem: Variants = {
    hidden: { opacity: 0, y: 10 },
    show: { opacity: 1, y: 0, transition: { duration: 0.35, ease: 'easeOut' } },
}

/** Shared viewport config for scroll-triggered `whileInView` reveals. */
export const inViewport = { once: true, margin: '-15%' } as const
