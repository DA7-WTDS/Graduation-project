import React, { useRef } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'motion/react'
import { RiskSurface } from '@/shared/visuals'
import { useScroll, useParallax } from '@/shared/motion/scroll'
import { staggerContainer, fadeInUp } from '@/shared/motion/variants'
import './Hero.css'

const Hero = () => {
    const ref = useRef(null)
    const { scrollYProgress } = useScroll({ target: ref, offset: ['start start', 'end start'] })
    const surfaceY = useParallax(scrollYProgress, 140)

    return (
        <section className="hero" ref={ref}>
            <div className="hero-grid" aria-hidden="true" />
            <motion.div className="hero-surface" style={{ y: surfaceY }} aria-hidden="true">
                <RiskSurface />
            </motion.div>

            <motion.div
                className="hero-inner"
                variants={staggerContainer}
                initial="hidden"
                animate="show"
            >
                <motion.span className="hero-eyebrow" variants={fadeInUp}>
                    AI-Powered Portfolio Advisory
                </motion.span>

                <motion.h1 className="hero-title" variants={fadeInUp}>
                    Build wealth with<br />
                    <span className="hero-accent">intelligent</span> portfolio management
                </motion.h1>

                <motion.p className="hero-subtitle" variants={fadeInUp}>
                    A daily, risk-graded run of market-wide predictions — personalized by an LLM into
                    BUY / SELL / HOLD picks and an allocation that fits your risk profile.
                </motion.p>

                <motion.div className="hero-cta" variants={fadeInUp}>
                    <Link to="/signup" className="hero-btn-primary">
                        Get started free <span aria-hidden="true">→</span>
                    </Link>
                    <a href="#how-it-works" className="hero-btn-secondary">See how it works</a>
                </motion.div>

                <motion.div className="hero-trust" variants={fadeInUp}>
                    <span className="hero-trust-item">Free to use</span>
                    <span className="hero-trust-item">Decision support, not auto-trading</span>
                    <span className="hero-trust-item">Risk-graded daily</span>
                </motion.div>
            </motion.div>
        </section>
    )
}

export default Hero
