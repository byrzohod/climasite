import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { SeoService } from './seo.service';

const EN = {
  seo: {
    default: { title: 'ClimaSite', description: 'Default site description' },
    page: { title: 'Page Title', description: 'Page description' }
  }
};
const BG = {
  seo: {
    default: { title: 'ClimaSite', description: 'Описание по подразбиране' },
    page: { title: 'Заглавие', description: 'Описание на страницата' }
  }
};

describe('SeoService', () => {
  let service: SeoService;
  let document: Document;
  let meta: Meta;
  let title: Title;
  let translate: TranslateService;
  let origin: string;

  function canonicalHref(): string | null {
    return document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')?.getAttribute('href') ?? null;
  }

  beforeEach(() => {
    // SeoService deliberately does NOT inject Router (that would be a circular DI with the
    // Router-constructed SeoTitleStrategy — NG0200); the default canonical path comes from
    // document.location.pathname, so no Router provider is needed here.
    TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot()]
    });

    translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', EN);
    translate.setTranslation('bg', BG);
    translate.use('en');

    service = TestBed.inject(SeoService);
    document = TestBed.inject(DOCUMENT);
    meta = TestBed.inject(Meta);
    title = TestBed.inject(Title);
    origin = document.location.origin;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('brand suffix (idempotency)', () => {
    it('appends the brand suffix exactly once', () => {
      service.setMeta({ title: 'Products' });
      expect(title.getTitle()).toBe('Products | ClimaSite');
    });

    it('does not double-suffix an already-branded title', () => {
      // Break-the-code probe: this assertion FAILS if buildTitle dropped its
      // `endsWith(BRAND_SUFFIX)` guard and blindly appended the suffix.
      service.setMeta({ title: 'Products | ClimaSite' });
      expect(title.getTitle()).toBe('Products | ClimaSite');
      expect(title.getTitle()).not.toBe('Products | ClimaSite | ClimaSite');
    });

    it('is idempotent across repeated key-based calls', () => {
      service.setMeta({ titleKey: 'seo.page.title' });
      service.setMeta({ titleKey: 'seo.page.title' });
      expect(title.getTitle()).toBe('Page Title | ClimaSite');
    });

    it('falls back to the bare brand when no title is supplied', () => {
      service.setMeta({});
      expect(title.getTitle()).toBe('ClimaSite');
    });
  });

  describe('canonical normalization', () => {
    it('strips the query string and fragment', () => {
      service.setMeta({ canonicalPath: '/products?page=2&sort=price#reviews' });
      expect(canonicalHref()).toBe(`${origin}/products`);
    });

    it('strips a trailing slash on non-root paths', () => {
      service.setMeta({ canonicalPath: '/about/' });
      expect(canonicalHref()).toBe(`${origin}/about`);
    });

    it('keeps the root path as "/"', () => {
      service.setMeta({ canonicalPath: '/' });
      expect(canonicalHref()).toBe(`${origin}/`);
    });

    it('defaults to the live document path when no canonicalPath is given', () => {
      // No Router dependency (avoids NG0200); the default path is document.location.pathname,
      // normalised (no trailing slash except root). Assert against the actual runner path.
      const expectedPath = document.location.pathname.replace(/\/+$/, '') || '/';
      service.setMeta({ title: 'X' });
      expect(canonicalHref()).toBe(`${origin}${expectedPath}`);
    });
  });

  describe('open graph / twitter / robots tags', () => {
    it('sets og:* and twitter:* and the canonical og:url', () => {
      service.setMeta({ title: 'Hello', description: 'World', type: 'product', canonicalPath: '/p/x' });

      expect(meta.getTag('property="og:title"')?.content).toBe('Hello | ClimaSite');
      expect(meta.getTag('property="og:description"')?.content).toBe('World');
      expect(meta.getTag('property="og:type"')?.content).toBe('product');
      expect(meta.getTag('property="og:url"')?.content).toBe(`${origin}/p/x`);
      expect(meta.getTag('property="og:site_name"')?.content).toBe('ClimaSite');
      expect(meta.getTag('name="twitter:card"')?.content).toBe('summary_large_image');
      expect(meta.getTag('name="twitter:title"')?.content).toBe('Hello | ClimaSite');
    });

    it('absolutizes a relative og:image against the origin', () => {
      service.setMeta({ image: '/assets/x.png' });
      expect(meta.getTag('property="og:image"')?.content).toBe(`${origin}/assets/x.png`);
      expect(meta.getTag('name="twitter:image"')?.content).toBe(`${origin}/assets/x.png`);
    });

    it('keeps an absolute og:image untouched', () => {
      service.setMeta({ image: 'https://cdn.example.com/x.png' });
      expect(meta.getTag('property="og:image"')?.content).toBe('https://cdn.example.com/x.png');
    });

    it('defaults robots to index,follow', () => {
      service.setMeta({ title: 'X' });
      expect(meta.getTag('name="robots"')?.content).toBe('index,follow');
    });

    it('emits the requested noindex robots value', () => {
      service.setMeta({ titleKey: 'seo.page.title', robots: 'noindex,follow' });
      expect(meta.getTag('name="robots"')?.content).toBe('noindex,follow');
    });
  });

  describe('language re-application', () => {
    it('re-resolves the current route title/description on language change', () => {
      service.setMeta({ titleKey: 'seo.page.title', descriptionKey: 'seo.page.description' });
      expect(title.getTitle()).toBe('Page Title | ClimaSite');

      translate.use('bg');

      expect(title.getTitle()).toBe('Заглавие | ClimaSite');
      expect(meta.getTag('name="description"')?.content).toBe('Описание на страницата');
    });
  });
});
