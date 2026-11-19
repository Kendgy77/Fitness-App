import axios from "axios";

/**
 * The one HTTP client of the app. In development VITE_API_URL is empty and Vite forwards /api to the API
 * (see vite.config.ts); in production it is the API's address.
 */
export const api = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL ?? ""}/api`,
  timeout: 15_000,
});

let accessToken: string | null = null;
let onUnauthorized: (() => void) | null = null;

/** Called by the auth provider whenever the user logs in or out. */
export function setAccessToken(token: string | null) {
  accessToken = token;
}

/** What to do when the API says the token is no longer valid (expired): the auth provider logs the user out. */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler;
}

// Every request carries the token, so pages never have to think about it.
api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

// A 401 while logged in means the token expired: log out. (A 401 on the login form itself is a wrong password,
// and there is no token yet, so it doesn't trigger this.)
api.interceptors.response.use(undefined, (error: unknown) => {
  if (axios.isAxiosError(error) && error.response?.status === 401 && accessToken) {
    onUnauthorized?.();
  }
  return Promise.reject(error);
});
