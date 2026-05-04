/**
 * Application-wide constants for routes and roles
 */

export const AppRoles = {
  Admin: 'Admin',
  Candidate: 'Candidate'
} as const;

export const AppRoutes = {
  auth: 'auth',
  login: 'login',
  register: 'register',
  admin: 'admin',
  candidate: 'candidate'
} as const;

export const AppRouteUrls = {
  authLogin: `/${AppRoutes.auth}/${AppRoutes.login}`,
  authRegister: `/${AppRoutes.auth}/${AppRoutes.register}`,
  admin: `/${AppRoutes.admin}`,
  candidate: `/${AppRoutes.candidate}`
} as const;
