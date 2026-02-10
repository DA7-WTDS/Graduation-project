import React from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import LandingPage from './pages/LandingPage/LandingPage'
import Login from './pages/Auth/Login'
import Signup from './pages/Auth/Signup'
import Onboarding from './pages/Onboarding/Onboarding'
import Dashboard from './pages/Dashboard/Dashboard'

function App() {
    return (
        <Router>
            <div className="App">
                <Routes>
                    <Route path="/" element={<LandingPage />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/signup" element={<Signup />} />
                    <Route path="/onboarding" element={<Onboarding />} />
                    <Route path="/dashboard" element={<Dashboard />} />
                </Routes>
            </div>
        </Router>
    )
}

export default App
