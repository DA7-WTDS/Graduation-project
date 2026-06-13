import { motion } from 'motion/react'
import type { ReactNode } from 'react'
import { fadeInUp, inViewport } from './variants'

/** Wraps a section so it fades/rises into view once on scroll (reduced-motion safe via Framer Motion). */
export function Reveal({ children, className }: { children: ReactNode; className?: string }) {
    return (
        <motion.div
            className={className}
            variants={fadeInUp}
            initial="hidden"
            whileInView="show"
            viewport={inViewport}
        >
            {children}
        </motion.div>
    )
}
