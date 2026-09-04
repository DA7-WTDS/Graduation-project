import React, { lazy, Suspense } from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import PrivateRoute from './components/PrivateRoute'
import AppShell from './app/AppShell'
import { LoadingState } from './shared/ui'

// Route-level code-splitting: each page (and the Framer Motion it pulls in) loads on demand.
const LandingPage = lazy(() => import('./pages/LandingPage/LandingPage'))
const Login = lazy(() => import('./pages/Auth/Login'))
const Signup = lazy(() => import('./pages/Auth/Signup'))
const Onboarding = lazy(() => import('./pages/Onboarding/Onboarding'))
const Dashboard = lazy(() => import('./pages/Dashboard/Dashboard'))
const Plan = lazy(() => import('./pages/Plan/Plan'))
const Profile = lazy(() => import('./pages/Profile/Profile'))
const Portfolios = lazy(() => import('./pages/Portfolios/Portfolios'))
const Simulator = lazy(() => import('./pages/Simulator/Simulator'))
const Market = lazy(() => import('./pages/Market/Market'))
const TrackRecord = lazy(() => import('./pages/TrackRecord/TrackRecord'))
const Methodology = lazy(() => import('./pages/Methodology/Methodology'))

const PageLoader = () => (
    <div style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'var(--qw-ink)'
    }}>
        <LoadingState label="Loading…" />
    </div>
)

function App() {
    return (
        <Router>
            <div className="App">
                <Suspense fallback={<PageLoader />}>
                    <Routes>
                        {/* Public */}
                        <Route path="/" element={<LandingPage />} />
                        <Route path="/login" element={<Login />} />
                        <Route path="/signup" element={<Signup />} />
                        {/* The trust surface is public on purpose: aggregates only, no
                            user or position data, and it has to be linkable to someone
                            who has not signed up (MVP_PLAN § 5). */}
                        <Route path="/track-record" element={<TrackRecord />} />
                        <Route path="/methodology" element={<Methodology />} />

                        {/* Authed, full-screen (no app shell) */}
                        <Route
                            path="/onboarding"
                            element={
                                <PrivateRoute>
                                    <Onboarding />
                                </PrivateRoute>
                            }
                        />

                        {/* Authed, inside the shared AppShell layout */}
                        <Route
                            element={
                                <PrivateRoute>
                                    <AppShell />
                                </PrivateRoute>
                            }
                        >
                            <Route path="/dashboard" element={<Dashboard />} />
                            <Route path="/plan" element={<Plan />} />
                            <Route path="/portfolios" element={<Portfolios />} />
                            <Route path="/simulator" element={<Simulator />} />
                            <Route path="/market" element={<Market />} />
                            <Route path="/profile" element={<Profile />} />
                        </Route>
                    </Routes>
                </Suspense>
            </div>
        </Router>
    )
}

export default App
