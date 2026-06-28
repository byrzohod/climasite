import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { RouterTestingModule } from '@angular/router/testing';
import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  let component: NotFoundComponent;
  let fixture: ComponentFixture<NotFoundComponent>;
  let host: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        NotFoundComponent,
        TranslateModule.forRoot(),
        RouterTestingModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(NotFoundComponent);
    component = fixture.componentInstance;
    host = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the 404 heading', () => {
    const h1 = host.querySelector('h1');
    expect(h1?.textContent?.trim()).toBe('404');
  });

  it('should render the not-found title and description (translation keys)', () => {
    const h2 = host.querySelector('h2');
    const p = host.querySelector('p');
    // With the default loader, the translate pipe echoes the key back.
    expect(h2?.textContent).toContain('errors.notFound');
    expect(p?.textContent).toContain('errors.notFoundDescription');
  });

  it('should render a home link pointing at the root route', () => {
    const link = host.querySelector('a.home-button') as HTMLAnchorElement | null;
    expect(link).toBeTruthy();
    expect(link?.getAttribute('href')).toBe('/');
    expect(link?.textContent).toContain('nav.home');
  });
});
