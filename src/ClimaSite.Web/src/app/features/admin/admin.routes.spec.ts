import { adminRoutes } from './admin.routes';

describe('admin routes', () => {
  it('matches the dashboard route only for /admin', () => {
    const dashboardRoute = adminRoutes.find(route => route.path === '');

    expect(dashboardRoute).toBeTruthy();
    expect(dashboardRoute?.pathMatch).toBe('full');
  });
});
