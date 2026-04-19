import { useMemo, useState } from 'react';
import { googleCallback, login, requestPhoneOtp, signup, verifyPhoneOtp } from '../services/authApi';

type Method = 'email' | 'google' | 'phone';

type Props = {
  onAuthenticated: (email: string) => void;
};

export function AuthScreen({ onAuthenticated }: Props) {
  const [method, setMethod] = useState<Method>('email');
  const [mode, setMode] = useState<'signup' | 'login'>('signup');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [phone, setPhone] = useState('');
  const [otp, setOtp] = useState('');
  const [googleCode, setGoogleCode] = useState('');
  const [redirectUri, setRedirectUri] = useState(window.location.origin);
  const [status, setStatus] = useState<string>('');
  const [resendAt, setResendAt] = useState<string>('');

  const canResend = useMemo(() => !resendAt || new Date(resendAt).getTime() <= Date.now(), [resendAt]);

  const handleEmail = async () => {
    const result = mode === 'signup' ? await signup(email, password, phone || undefined) : await login(email, password);
    localStorage.setItem('traceai_auth', JSON.stringify(result));
    onAuthenticated(result.user.email);
    setStatus(`Authenticated. Email verified: ${result.user.emailVerified}. OAuth: ${result.user.oAuthProvider ?? 'none'}`);
  };

  const handleGoogle = async () => {
    const result = await googleCallback(googleCode, redirectUri);
    localStorage.setItem('traceai_auth', JSON.stringify(result));
    onAuthenticated(result.user.email);
    setStatus(`Google linked. Email verified: ${result.user.emailVerified}.`);
  };

  const handleOtpRequest = async () => {
    const challenge = await requestPhoneOtp(email, phone);
    setResendAt(challenge.resendAvailableAtUtc);
    setStatus(`OTP sent. Expires at ${new Date(challenge.expiresAtUtc).toLocaleTimeString()}. Max attempts: ${challenge.maxAttempts}`);
  };

  const handleOtpVerify = async () => {
    const profile = await verifyPhoneOtp(email, phone, otp);
    setStatus(`Phone verified: ${profile.phoneVerified}`);
    if (profile.phoneVerified) {
      onAuthenticated(profile.email);
    }
  };

  return (
    <section className="card content">
      <h2>Unified Authentication</h2>
      <p className="muted">Choose Email/Password, Google OAuth callback exchange, or Phone OTP verification.</p>

      <div className="nav-grid">
        {[
          ['email', 'Email/Password'],
          ['google', 'Google OAuth'],
          ['phone', 'Phone OTP']
        ].map(([key, label]) => (
          <button key={key} className={method === key ? 'nav-button active' : 'nav-button'} onClick={() => setMethod(key as Method)}>
            {label}
          </button>
        ))}
      </div>

      {method === 'email' && (
        <>
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="user@example.com" />
          <label>Password</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" />
          <label>Phone (optional on signup)</label>
          <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+15551234567" />
          <div className="row">
            <button onClick={() => setMode('signup')}>Set Signup</button>
            <button onClick={() => setMode('login')}>Set Login</button>
            <button onClick={handleEmail}>{mode === 'signup' ? 'Signup' : 'Login'}</button>
          </div>
        </>
      )}

      {method === 'google' && (
        <>
          <label>Google OAuth authorization code</label>
          <input value={googleCode} onChange={(e) => setGoogleCode(e.target.value)} placeholder="Paste ?code=... from callback" />
          <label>Redirect URI</label>
          <input value={redirectUri} onChange={(e) => setRedirectUri(e.target.value)} />
          <button onClick={handleGoogle}>Exchange callback and sign in</button>
        </>
      )}

      {method === 'phone' && (
        <>
          <label>Email</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} />
          <label>Phone</label>
          <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+15551234567" />
          <div className="row">
            <button onClick={handleOtpRequest} disabled={!canResend}>Send / Resend OTP</button>
            <p className="muted">Resend available: {resendAt ? new Date(resendAt).toLocaleTimeString() : 'now'}</p>
          </div>
          <label>OTP code</label>
          <input value={otp} onChange={(e) => setOtp(e.target.value)} placeholder="123456" />
          <button onClick={handleOtpVerify}>Verify OTP</button>
        </>
      )}

      {status && <p className="muted">{status}</p>}
    </section>
  );
}
