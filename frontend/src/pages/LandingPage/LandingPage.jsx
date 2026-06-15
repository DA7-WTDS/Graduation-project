import React from 'react'
import Navbar from '../../components/Navbar/Navbar'
import Hero from '../../components/Hero/Hero'
import Features from '../../components/Features/Features'
import HowItWorks from '../../components/HowItWorks/HowItWorks'
import About from '../../components/About/About'
import Footer from '../../components/Footer/Footer'
import { Reveal } from '@/shared/motion/Reveal'
import './LandingPage.css'

const LandingPage = () => {
    return (
        <div className="landing-page">
            <Navbar />
            <Hero />
            <Reveal><Features /></Reveal>
            <Reveal><HowItWorks /></Reveal>
            <Reveal><About /></Reveal>
            <Footer />
        </div>
    )
}

export default LandingPage
