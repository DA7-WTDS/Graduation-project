import React from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import LandingPage from './pages/LandingPage/LandingPage'
import Login from './pages/Auth/Login'
import Signup from './pages/Auth/Signup'
import Onboarding from './pages/Onboarding/Onboarding'
import Dashboard from './pages/Dashboard/Dashboard'
import Profile from './pages/Profile/Profile'
import Portfolios from './pages/Portfolios/Portfolios'
import Simulator from './pages/Simulator/Simulator'
import Market from './pages/Market/Market'
import PrivateRoute from './components/PrivateRoute'
import AppShell from './app/AppShell'

function App() {
    return (
        <Router>
            <div className="App">
                <Routes>
                    {/* Public */}
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/signup" element={<Signup />} />

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
                        <Route path="/portfolios" element={<Portfolios />} />
                        <Route path="/simulator" element={<Simulator />} />
                        <Route path="/market" element={<Market />} />
                        <Route path="/profile" element={<Profile />} />
                    </Route>
                </Routes>
            </div>
        </Router>
    )
}

export default App
