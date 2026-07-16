import { useState } from 'react'
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Menu, X, User, LogOut } from 'lucide-react'
import { useAuth } from '@/context/AuthContext'
import { NotificationBell } from '@/features/notifications/NotificationBell'
import { Backdrop } from '@/shared/visuals'
import { useToast } from '@/shared/ui'
import { useLanguage } from '@/shared/i18n'
import type { UserProfile } from '@/types/api'
import './AppShell.css'

const navItems = [
    { to: '/dashboard', key: 'nav.dashboard' },
    { to: '/plan', key: 'nav.plan' },
    { to: '/portfolios', key: 'nav.portfolios' },
    { to: '/simulator', key: 'nav.learning' },
    { to: '/market', key: 'nav.market' },
]

/**
 * Shared authed layout: the single app chrome (wordmark + nav + bell + avatar)
 * wrapping every protected route via react-router's nested <Outlet/>.
 * Replaces the 5× duplicated inline headers.
 */
export default function AppShell() {
    const { user, logout } = useAuth() as { user: UserProfile | null; logout: () => void }
    const navigate = useNavigate()
    const toast = useToast()
    const { t, toggle, lang } = useLanguage()
    const initials = user ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}` : '?'

    const [mobileNavOpen, setMobileNavOpen] = useState(false)
    const [accountOpen, setAccountOpen] = useState(false)

    const handleLogout = () => {
        setAccountOpen(false)
        logout()
        toast.info('Signed out')
        navigate('/')
    }

    return (
        <div className="app-shell">
            <Backdrop fixed surface />

            <header className="app-nav">
                <button
                    type="button"
                    className="app-menu-toggle"
                    aria-label="Toggle navigation menu"
                    aria-expanded={mobileNavOpen}
                    onClick={() => setMobileNavOpen((v) => !v)}
                >
                    {mobileNavOpen ? <X size={18} /> : <Menu size={18} />}
                </button>

                <Link to="/dashboard" className="app-wordmark">
                    QUANTWISE
                </Link>

                <nav className="app-nav-links">
                    {navItems.map((item) => (
                        <NavLink
                            key={item.to}
                            to={item.to}
                            className={({ isActive }) => `app-nav-link${isActive ? ' active' : ''}`}
                        >
                            {t(item.key)}
                        </NavLink>
                    ))}
                </nav>

                <div className="app-nav-right">
                    <button
                        type="button"
                        className="app-lang-toggle"
                        onClick={toggle}
                        title={t('lang.switch')}
                        aria-label={`Switch language to ${lang === 'ar' ? 'English' : 'Arabic'}`}
                    >
                        {t('lang.switch')}
                    </button>
                    <NotificationBell />
                    <div className="app-account">
                        <button
                            type="button"
                            className="app-avatar"
                            title="Account"
                            aria-haspopup="menu"
                            aria-expanded={accountOpen}
                            onClick={() => setAccountOpen((v) => !v)}
                        >
                            {initials || '?'}
                        </button>

                        {accountOpen && (
                            <>
                                <div className="app-menu-overlay" onClick={() => setAccountOpen(false)} />
                                <div className="app-account-menu" role="menu">
                                    <div className="app-account-head">
                                        <span className="app-account-name">
                                            {user ? `${user.firstName} ${user.lastName}` : t('nav.account')}
                                        </span>
                                        {user?.email && <span className="app-account-email">{user.email}</span>}
                                    </div>
                                    <Link
                                        to="/profile"
                                        className="app-menu-item"
                                        role="menuitem"
                                        onClick={() => setAccountOpen(false)}
                                    >
                                        <User size={15} strokeWidth={1.75} /> {t('nav.profile')}
                                    </Link>
                                    <button
                                        type="button"
                                        className="app-menu-item danger"
                                        role="menuitem"
                                        onClick={handleLogout}
                                    >
                                        <LogOut size={15} strokeWidth={1.75} /> {t('nav.logout')}
                                    </button>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </header>

            {mobileNavOpen && (
                <>
                    <div className="app-menu-overlay" onClick={() => setMobileNavOpen(false)} />
                    <nav className="app-mobile-nav">
                        {navItems.map((item) => (
                            <NavLink
                                key={item.to}
                                to={item.to}
                                className={({ isActive }) => `app-mobile-link${isActive ? ' active' : ''}`}
                                onClick={() => setMobileNavOpen(false)}
                            >
                                {t(item.key)}
                            </NavLink>
                        ))}
                    </nav>
                </>
            )}

            <main className="app-main">
                <Outlet />
            </main>
        </div>
    )
}
