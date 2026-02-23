import React from 'react'
import { Link } from 'react-router-dom'
import './Portfolios.css'

const Portfolios = () => {
    return (
        <div className="portfolios-page">
            <header className="page-header">
                <div className="header-content">
                    <Link to="/dashboard" className="back-link">← Dashboard</Link>
                    <div className="header-logo"><span className="gradient-text">SmartInvest</span> AI</div>
                    <div className="header-actions">
                        <span className="user-badge">Management</span>
                    </div>
                </div>
            </header>

            <div className="portfolios-body">
                <div className="portfolios-hero">
                    <h1 className="gradient-text">My Portfolios</h1>
                    <p>Manage and analyze all your investment accounts in one place.</p>
                </div>

                <div className="portfolios-placeholder">
                    <div className="icon">📁</div>
                    <h3>Portfolio Management coming soon</h3>
                    <p>We're finishing the cross-portfolio analysis engine.</p>
                </div>
            </div>
        </div>
    )
}

export default Portfolios
