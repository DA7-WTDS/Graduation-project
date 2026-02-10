import React, { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import './Navbar.css'

const Navbar = () => {
    const [isScrolled, setIsScrolled] = useState(false)

    useEffect(() => {
        const handleScroll = () => {
            setIsScrolled(window.scrollY > 50)
        }

        window.addEventListener('scroll', handleScroll)
        return () => window.removeEventListener('scroll', handleScroll)
    }, [])

    const scrollToSection = (sectionId) => {
        const element = document.getElementById(sectionId)
        if (element) {
            const offset = 80 // navbar height
            const elementPosition = element.getBoundingClientRect().top
            const offsetPosition = elementPosition + window.pageYOffset - offset

            window.scrollTo({
                top: offsetPosition,
                behavior: 'smooth'
            })
        }
    }

    return (
        <nav className={`navbar ${isScrolled ? 'solid' : 'transparent'}`}>
            <div className="navbar-container">
                <div className="navbar-logo">
                    <span className="logo-gradient">SmartInvest</span> AI
                </div>

                <ul className="navbar-links">
                    <li className="navbar-link" onClick={() => scrollToSection('features')}>Features</li>
                    <li className="navbar-link" onClick={() => scrollToSection('how-it-works')}>How It Works</li>
                    <li className="navbar-link" onClick={() => scrollToSection('pricing')}>Pricing</li>
                    <li className="navbar-link" onClick={() => scrollToSection('about')}>About</li>
                </ul>

                <Link to="/signup">
                    <button className="navbar-cta">Get Started</button>
                </Link>

                <div className="navbar-mobile-toggle">
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>
        </nav>
    )
}

export default Navbar
