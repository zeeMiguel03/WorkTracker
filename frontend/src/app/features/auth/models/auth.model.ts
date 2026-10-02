export interface User {
  id: number;
  name: string;
  email: string;
  profileImageUrl: string | null;
  createdAt: string;
  utCreation: number | null;
  hasLocalPassword: boolean;
  hasGoogleLogin: boolean;
}

export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}
