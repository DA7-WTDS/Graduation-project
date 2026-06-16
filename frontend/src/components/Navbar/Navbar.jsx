import React, { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import './Navbar.css'

const Navbar = () => {
    const [isScrolled, setIsScrolled] = useState(false)
    const [menuOpen, setMenuOpen] = useState(false)

    useEffect(() => {
        const handleScroll = () => {
            setIsScrolled(window.scrollY > 50)
        }

        window.addEventListener('scroll', handleScroll)
        return () => window.removeEventListener('scroll', handleScroll)
    }, [])

    const scrollToSection = (sectionId) => {
        setMenuOpen(false)
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
                    <span className="logo-gradient">QuantWise</span>
                </div>

                <ul className="navbar-links">
                    <li className="navbar-link" onClick={() => scrollToSection('features')}>Features</li>
                    <li className="navbar-link" onClick={() => scrollToSection('how-it-works')}>How It Works</li>
                    <li className="navbar-link" onClick={() => scrollToSection('about')}>About</li>
                </ul>

                <div className="navbar-actions">
                    <Link to="/login" className="navbar-login">Log In</Link>
                    <Link to="/signup" className="navbar-cta-link">
                        <button className="navbar-cta">Get Started</button>
                    </Link>
                </div>

                <button
                    type="button"
                    className={`navbar-mobile-toggle${menuOpen ? ' open' : ''}`}
                    aria-label="Toggle menu"
                    aria-expanded={menuOpen}
                    onClick={() => setMenuOpen((v) => !v)}
                >
                    <span></span>
                    <span></span>
                    <span></span>
                </button>
            </div>

            {menuOpen && (
                <div className="navbar-mobile-menu">
                    <button className="navbar-mobile-link" onClick={() => scrollToSection('features')}>Features</button>
                    <button className="navbar-mobile-link" onClick={() => scrollToSection('how-it-works')}>How It Works</button>
                    <button className="navbar-mobile-link" onClick={() => scrollToSection('about')}>About</button>
                    <Link to="/login" className="navbar-mobile-link" onClick={() => setMenuOpen(false)}>Log In</Link>
                    <Link to="/signup" className="navbar-mobile-cta" onClick={() => setMenuOpen(false)}>Get Started</Link>
                </div>
            )}
        </nav>
    )
}

export default Navbar
