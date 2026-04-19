export type UserProfile = {
  email: string;
  emailVerified: boolean;
  phoneVerified: boolean;
  phoneNumber?: string | null;
  oAuthProvider?: string | null;
};

export type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  user: UserProfile;
};

export type OtpChallengeResponse = {
  expiresAtUtc: string;
  resendAvailableAtUtc: string;
  maxAttempts: number;
};

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api';

async function post<T>(path: string, payload: unknown): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export const signup = (email: string, password: string, phoneNumber?: string) =>
  post<AuthResponse>('/auth/signup', { email, password, phoneNumber });

export const login = (email: string, password: string) => post<AuthResponse>('/auth/login', { email, password });

export const refresh = (refreshToken: string) => post<AuthResponse>('/auth/refresh', { refreshToken });

export const googleCallback = (code: string, redirectUri: string) =>
  post<AuthResponse>('/auth/google/callback', { code, redirectUri });

export const requestPhoneOtp = (email: string, phoneNumber: string) =>
  post<OtpChallengeResponse>('/auth/phone/request-otp', { email, phoneNumber });

export const verifyPhoneOtp = (email: string, phoneNumber: string, code: string) =>
  post<UserProfile>('/auth/phone/verify', { email, phoneNumber, code });
