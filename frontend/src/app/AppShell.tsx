import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { NotificationBell } from '@/features/notifications/NotificationBell'
import type { UserProfile } from '@/types/api'
import './AppShell.css'

const navItems = [
    { to: '/dashboard', label: 'Dashboard' },
    { to: '/portfolios', label: 'Portfolios' },
    { to: '/simulator', label: 'Learning' },
    { to: '/market', label: 'Market' },
]

/**
 * Shared authed layout: the single app chrome (wordmark + nav + bell + avatar)
 * wrapping every protected route via react-router's nested <Outlet/>.
 * Replaces the 5× duplicated inline headers.
 */
export default function AppShell() {
    const { user } = useAuth() as { user: UserProfile | null }
    const initials = user ? `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}` : '?'

    return (
        <div className="app-shell">
            <header className="app-nav">
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
                            {item.label}
                        </NavLink>
                    ))}
                </nav>

                <div className="app-nav-right">
                    <NotificationBell />
                    <Link to="/profile" className="app-avatar" title="Profile">
                        {initials || '?'}
                    </Link>
                </div>
            </header>

            <main className="app-main">
                <Outlet />
            </main>
        </div>
    )
}
