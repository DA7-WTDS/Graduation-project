import React from 'react'
import './Footer.css'

const Footer = () => {
    return (
        <footer className="footer">
            <div className="footer-container">
                <div className="footer-content">
                    <div className="footer-brand">
                        <div className="footer-logo">
                            <span className="gradient-text">SmartInvest</span> AI
                        </div>
                        <p className="footer-tagline">
                            AI-powered investment management for everyone
                        </p>
                        <div className="footer-social">
                            <div className="social-icon">in</div>
                            <div className="social-icon">𝕏</div>
                            <div className="social-icon">f</div>
                        </div>
                    </div>

                    <div className="footer-column">
                        <h4>Product</h4>
                        <ul className="footer-links">
                            <li className="footer-link">Features</li>
                            <li className="footer-link">Pricing</li>
                            <li className="footer-link">How It Works</li>
                            <li className="footer-link">Security</li>
                        </ul>
                    </div>

                    <div className="footer-column">
                        <h4>Company</h4>
                        <ul className="footer-links">
                            <li className="footer-link">About Us</li>
                            <li className="footer-link">Blog</li>
                            <li className="footer-link">Careers</li>
                            <li className="footer-link">Press Kit</li>
                        </ul>
                    </div>

                    <div className="footer-column">
                        <h4>Resources</h4>
                        <ul className="footer-links">
                            <li className="footer-link">Help Center</li>
                            <li className="footer-link">API Docs</li>
                            <li className="footer-link">Terms of Service</li>
                            <li className="footer-link">Privacy Policy</li>
                        </ul>
                    </div>
                </div>

                <div className="footer-bottom">
                    <div className="footer-copyright">
                        © 2026 SmartInvest AI. All rights reserved.
                    </div>
                    <div className="footer-legal">
                        <span>Securities offered through regulated brokers</span>
                    </div>
                </div>
            </div>
        </footer>
    )
}

export default Footer
