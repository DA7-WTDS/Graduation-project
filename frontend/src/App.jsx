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

function App() {
    return (
        <Router>
            <div className="App">
                <Routes>
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/signup" element={<Signup />} />
                    <Route
                        path="/onboarding"
                        element={
                            <PrivateRoute>
                                <Onboarding />
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/dashboard"
                        element={
                            <PrivateRoute>
                                <Dashboard />
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/portfolios"
                        element={
                            <PrivateRoute>
                                <Portfolios />
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/simulator"
                        element={
                            <PrivateRoute>
                                <Simulator />
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/market"
                        element={
                            <PrivateRoute>
                                <Market />
                            </PrivateRoute>
                        }
                    />
                    <Route
                        path="/profile"
                        element={
                            <PrivateRoute>
                                <Profile />
                            </PrivateRoute>
                        }
                    />
                </Routes>
            </div>
        </Router>
    )
}

export default App
