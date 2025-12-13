import React, { useState } from 'react';
import './Login.css';

const Login = ({ onLogin }) => {
  const [credentials, setCredentials] = useState({
    username: '',
    password: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setCredentials(prev => ({
      ...prev,
      [name]: value
    }));
    // Clear error when user starts typing
    if (error) setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Simple validation - in real app, this would be API call
      if (credentials.username === 'admin' && credentials.password === 'peksan2024') {
        onLogin(credentials.username);
      } else {
        setError('Kullanıcı adı veya şifre hatalı!');
      }
    } catch (err) {
      setError('Giriş yapılırken bir hata oluştu!');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-background">
        <div className="login-card">
          <div className="login-header">
            <img
              src="/peksan_logo.jpg"
              alt="Peksan Logo"
              className="login-logo"
              onError={(e) => {
                // JPG yüklenemezse SVG'ye fallback
                e.target.src = "/peksan-logo.svg";
              }}
            />
            <h1 className="login-title">İzlenebilirlik Sistemi</h1>
            <p className="login-subtitle">Lütfen giriş bilgilerinizi giriniz</p>
          </div>

          <form onSubmit={handleSubmit} className="login-form">
            <div className="form-group">
              <label htmlFor="username" className="form-label">
                Kullanıcı Adı
              </label>
              <input
                type="text"
                id="username"
                name="username"
                value={credentials.username}
                onChange={handleInputChange}
                className="form-input"
                placeholder="Kullanıcı adınızı giriniz"
                required
                disabled={isLoading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="password" className="form-label">
                Şifre
              </label>
              <input
                type="password"
                id="password"
                name="password"
                value={credentials.password}
                onChange={handleInputChange}
                className="form-input"
                placeholder="Şifrenizi giriniz"
                required
                disabled={isLoading}
              />
            </div>

            {error && (
              <div className="error-message">
                <i className="error-icon">⚠️</i>
                {error}
              </div>
            )}

            <button 
              type="submit" 
              className={`login-button ${isLoading ? 'loading' : ''}`}
              disabled={isLoading}
            >
              {isLoading ? (
                <>
                  <span className="spinner"></span>
                  Giriş yapılıyor...
                </>
              ) : (
                'Giriş Yap'
              )}
            </button>
          </form>

          <div className="login-footer">
            <p className="demo-info">
              <strong>Demo Bilgileri:</strong><br />
              Kullanıcı Adı: <code>admin</code><br />
              Şifre: <code>peksan2024</code>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
