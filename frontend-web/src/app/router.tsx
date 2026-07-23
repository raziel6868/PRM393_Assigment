import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';
import { AuthProvider } from './auth-context';
import { AnonymousGuard, RoleGuard, SessionGuard } from './guards';
import { AuthenticatedShell } from './shell';
import { LoginPage } from '../features/auth/login-page';
import { PasswordHelpPage } from '../features/auth/password-help-page';
import { ChangeTemporaryPasswordPage } from '../features/auth/change-temporary-password-page';
import { DashboardPage } from '../features/dashboard/dashboard-page';
import { PasswordHelpRequestsPage } from '../features/password-help/password-help-requests-page';
import { ImportsPage } from '../features/imports/imports-page';

const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <SessionGuard>
        <AuthenticatedShell />
      </SessionGuard>
    ),
    children: [
      { index: true, element: <Navigate to="/dashboard" replace /> },
      { path: 'dashboard', element: <DashboardPage /> },
      {
        path: 'password-help-requests',
        element: (
          <RoleGuard roles={['administrator']}>
            <PasswordHelpRequestsPage />
          </RoleGuard>
        ),
      },
      {
        path: 'imports',
        element: (
          <RoleGuard roles={['administrator']}>
            <ImportsPage />
          </RoleGuard>
        ),
      },
    ],
  },
  {
    path: '/login',
    element: (
      <AnonymousGuard>
        <LoginPage />
      </AnonymousGuard>
    ),
  },
  {
    path: '/password-help',
    element: (
      <AnonymousGuard>
        <PasswordHelpPage />
      </AnonymousGuard>
    ),
  },
  {
    path: '/change-temporary-password',
    element: (
      <SessionGuard>
        <ChangeTemporaryPasswordPage />
      </SessionGuard>
    ),
  },
  { path: '*', element: <Navigate to="/" replace /> },
]);

export function App(): JSX.Element {
  return (
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  );
}
