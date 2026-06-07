import { routes } from './app.routes';

describe('app routes', () => {
  it('matches the home route only for the root URL', () => {
    const homeRoute = routes.find(route => route.path === '');

    expect(homeRoute).toBeTruthy();
    expect(homeRoute?.pathMatch).toBe('full');
    expect(homeRoute?.loadComponent).toEqual(jasmine.any(Function));
    expect(homeRoute?.component).toBeUndefined();
  });
});
