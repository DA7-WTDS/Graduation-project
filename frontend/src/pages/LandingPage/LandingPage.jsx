import React from 'react'
import Navbar from '../../components/Navbar/Navbar'
import Hero from '../../components/Hero/Hero'
import Features from '../../components/Features/Features'
import HowItWorks from '../../components/HowItWorks/HowItWorks'
import Pricing from '../../components/Pricing/Pricing'
import About from '../../components/About/About'
import Footer from '../../components/Footer/Footer'

const LandingPage = () => {
    return (
        <div className="landing-page">
            <Navbar />
            <Hero />
            <Features />
            <HowItWorks />
            <Pricing />
            <About />
            <Footer />
        </div>
    )
}

export default LandingPage
