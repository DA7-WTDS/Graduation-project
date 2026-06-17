import React from 'react'
import { Link } from 'react-router-dom'
import './Footer.css'

const REPO_URL = 'https://github.com/DA7-WTDS/Graduation-project'

const GithubMark = () => (
    <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor" aria-hidden="true">
        <path d="M12 .5C5.73.5.5 5.73.5 12c0 5.08 3.29 9.39 7.86 10.91.58.11.79-.25.79-.56 0-.28-.01-1.02-.02-2-3.2.7-3.88-1.54-3.88-1.54-.52-1.33-1.28-1.69-1.28-1.69-1.05-.72.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.7 1.26 3.36.96.1-.75.4-1.26.73-1.55-2.55-.29-5.23-1.28-5.23-5.7 0-1.26.45-2.29 1.19-3.1-.12-.29-.52-1.46.11-3.05 0 0 .97-.31 3.18 1.18a11 11 0 0 1 5.79 0c2.2-1.49 3.17-1.18 3.17-1.18.63 1.59.23 2.76.11 3.05.74.81 1.19 1.84 1.19 3.1 0 4.43-2.69 5.41-5.25 5.69.41.36.77 1.05.77 2.12 0 1.53-.01 2.77-.01 3.15 0 .31.21.68.8.56A10.52 10.52 0 0 0 23.5 12C23.5 5.73 18.27.5 12 .5z" />
    </svg>
)

const Footer = () => {
    return (
        <footer className="footer">
            <div className="footer-container">
                <div className="footer-content">
                    <div className="footer-brand">
                        <div className="footer-logo">
                            <span className="gradient-text">QuantWise</span>
                        </div>
                        <p className="footer-tagline">
                            AI-powered, risk-graded investment recommendations.
                        </p>
                        <a
                            className="footer-social-link"
                            href={REPO_URL}
                            target="_blank"
                            rel="noreferrer"
                            aria-label="GitHub repository"
                        >
                            <GithubMark />
                            <span>GitHub</span>
                        </a>
                    </div>

                    <div className="footer-column">
                        <h4>Explore</h4>
                        <ul className="footer-links">
                            <li><a className="footer-link" href="#features">Features</a></li>
                            <li><a className="footer-link" href="#how-it-works">How It Works</a></li>
                            <li><a className="footer-link" href="#about">About</a></li>
                        </ul>
                    </div>

                    <div className="footer-column">
                        <h4>Get Started</h4>
                        <ul className="footer-links">
                            <li><Link className="footer-link" to="/login">Log In</Link></li>
                            <li><Link className="footer-link" to="/signup">Create Account</Link></li>
                        </ul>
                    </div>
                </div>

                <div className="footer-bottom">
                    <div className="footer-copyright">© 2026 QuantWise</div>
                    <div className="footer-legal">
                        <span>Informational only — not financial advice.</span>
                    </div>
                </div>
            </div>
        </footer>
    )
}

export default Footer
