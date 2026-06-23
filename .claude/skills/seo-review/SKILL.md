---
name: seo-review
description: SEO audit checklist for public-facing web apps -- meta tags (title, description), Open Graph, canonical URLs, structured data (JSON-LD), sitemap.xml, robots.txt, redirects (301 not 302), URL structure, Core Web Vitals impact on ranking. Use this whenever the user mentions SEO, search engine optimization, organic traffic, Google ranking, meta description, Open Graph, schema markup, sitemap, or any public marketing/landing page review.
---

# /seo-review - SEO audit checklist

## When to use

Invoke this skill:
- **Before launching a public-facing web app** -- full SEO pass
- **After adding new pages or routes** -- verify SEO basics are in place
- **Before every production release** -- spot-check for regressions
- **When organic traffic matters** -- not needed for internal tools or admin panels

This skill applies to public-facing web applications where search engine visibility matters. Skip for internal tools, APIs without a frontend, or apps behind authentication.

## How to run this skill

1. Read the current state of the frontend code and rendered pages
2. Go through each checklist section systematically
3. For each item, mark: ✓ verified, ✗ failed, or N/A
4. For every failure, propose and apply a fix
5. Re-verify all failed items
6. Generate a summary report

## SEO Checklist

### Rendering & Crawlability

- [ ] Pages are server-side rendered (SSR) or statically generated -- not client-only SPA (search engines struggle with JavaScript-only rendering)
- [ ] `robots.txt` exists at the root and allows crawling of public pages
- [ ] `robots.txt` blocks crawling of admin, API, auth, and internal routes
- [ ] XML sitemap exists at `/sitemap.xml` and is referenced in `robots.txt`
- [ ] Sitemap includes all public pages with `<lastmod>` dates
- [ ] Sitemap auto-updates when pages are added or removed
- [ ] No `noindex` meta tags on pages that should be indexed
- [ ] No `nofollow` on internal links that should pass authority
- [ ] JavaScript-rendered content is accessible to crawlers (check with "View Source" or `curl` -- if the content isn't in the HTML, crawlers can't see it)

### Meta Tags (every page)

- [ ] `<title>` tag present, unique per page, 50-60 characters, includes primary keyword
- [ ] `<meta name="description">` present, unique per page, 150-160 characters, compelling (this is the search result snippet)
- [ ] `<meta name="viewport" content="width=device-width, initial-scale=1">` present
- [ ] `<link rel="canonical" href="...">` present on every page pointing to the preferred URL (prevents duplicate content)
- [ ] No duplicate titles or descriptions across different pages
- [ ] Dynamic pages (product pages, blog posts) generate unique titles and descriptions from content

### Open Graph & Social Sharing

- [ ] `og:title` -- matches or improves on the `<title>` tag
- [ ] `og:description` -- compelling summary for social feeds
- [ ] `og:image` -- high-quality image, minimum 1200x630px (this is what shows in link previews)
- [ ] `og:url` -- canonical URL
- [ ] `og:type` -- `website`, `article`, etc.
- [ ] `og:site_name` -- brand name
- [ ] `twitter:card` -- `summary_large_image` for rich previews
- [ ] `twitter:title`, `twitter:description`, `twitter:image` -- set (or falls back to OG tags)
- [ ] Test with: Facebook Sharing Debugger, Twitter Card Validator, LinkedIn Post Inspector

### Structured Data (JSON-LD)

- [ ] JSON-LD `<script type="application/ld+json">` in `<head>` for relevant pages
- [ ] **Organization** schema on the homepage (name, logo, social profiles, contact)
- [ ] **WebSite** schema with `SearchAction` if the site has search
- [ ] **BreadcrumbList** schema on pages with breadcrumbs
- [ ] **Article** schema on blog posts (headline, datePublished, author, image)
- [ ] **Product** schema on product pages (name, price, availability, reviews)
- [ ] **FAQPage** schema on FAQ pages
- [ ] **LocalBusiness** schema if applicable (address, hours, phone)
- [ ] Validate with Google Rich Results Test (https://search.google.com/test/rich-results)

### URL Structure

- [ ] URLs are clean, readable, and descriptive (`/blog/seo-best-practices` not `/post?id=42`)
- [ ] URLs use hyphens, not underscores (`my-page` not `my_page`)
- [ ] URLs are lowercase
- [ ] No trailing slashes inconsistency (pick one convention and enforce it)
- [ ] No URL parameters for content that should be indexed (use path segments instead)
- [ ] 301 redirects for any changed URLs (never let old URLs 404)
- [ ] No redirect chains (A -> B -> C -- should be A -> C)

### Heading Hierarchy

- [ ] Exactly one `<h1>` per page containing the primary keyword
- [ ] Heading hierarchy is sequential (h1 -> h2 -> h3, no skipping levels)
- [ ] Headings describe the content structure (not just styled for size)
- [ ] Primary keyword appears naturally in at least one `<h2>`

### Images

- [ ] All images have descriptive `alt` text (not filename, not empty on meaningful images)
- [ ] Image file names are descriptive (`kitchen-renovation.jpg` not `IMG_4532.jpg`)
- [ ] Images are optimized (WebP/AVIF format, appropriate dimensions, compressed)
- [ ] Images have `width` and `height` attributes (prevents CLS)
- [ ] Above-the-fold images are NOT lazy-loaded (hurts LCP)
- [ ] Below-the-fold images use `loading="lazy"`
- [ ] Large hero images use `<link rel="preload">` for faster LCP

### Performance (Core Web Vitals)

Core Web Vitals directly impact search ranking:

- [ ] **Largest Contentful Paint (LCP)** < 2.5s (measure with Lighthouse or PageSpeed Insights)
- [ ] **Interaction to Next Paint (INP)** < 200ms
- [ ] **Cumulative Layout Shift (CLS)** < 0.1
- [ ] No render-blocking JavaScript or CSS in `<head>` (defer or async)
- [ ] Fonts preloaded with `<link rel="preload">` and `font-display: swap`
- [ ] Third-party scripts (analytics, chat widgets) loaded asynchronously
- [ ] HTTP/2 or HTTP/3 enabled
- [ ] Gzip/Brotli compression enabled
- [ ] Bundle size reasonable (code splitting for routes)

### Mobile

- [ ] Site is fully responsive (Google uses mobile-first indexing)
- [ ] Touch targets at least 48x48px with 8px spacing
- [ ] Text readable without zooming (minimum 16px body text)
- [ ] No horizontal scrolling at any mobile viewport
- [ ] Mobile page speed tested separately (often worse than desktop)
- [ ] No intrusive interstitials (popups that cover content on mobile hurt ranking)

### Internal Linking

- [ ] Key pages are reachable within 3 clicks from the homepage
- [ ] Related content is cross-linked (blog posts link to related posts, products link to related products)
- [ ] Navigation includes all important sections
- [ ] Breadcrumbs present on multi-level pages
- [ ] No orphan pages (pages with no internal links pointing to them)
- [ ] Anchor text is descriptive (not "click here" -- use the target page's topic)

### Technical

- [ ] HTTPS enforced (HTTP -> HTTPS redirect)
- [ ] `<html lang="en">` (or appropriate language) set correctly
- [ ] `hreflang` tags for multi-language sites (tells Google which language version to show per region)
- [ ] 404 page exists with navigation and search
- [ ] No soft 404s (pages that return 200 but show "not found" content)
- [ ] Page load time under 3 seconds on 3G connection
- [ ] No mixed content warnings (HTTP resources on HTTPS pages)
- [ ] Clean HTML (no excessive DOM depth, reasonable element count)

### Content Quality Signals

- [ ] Every indexable page has substantial, unique content (not thin or duplicate)
- [ ] Blog posts have publication dates visible
- [ ] Author information present on articles (E-E-A-T signal)
- [ ] Content is original (not copied from other sources)
- [ ] No keyword stuffing (natural language, not forced repetition)
- [ ] Content answers the user's search intent (informational, navigational, transactional)

### Monitoring & Analytics

- [ ] Analytics configured (privacy-respecting: Plausible, Umami, PostHog -- or Google Analytics if preferred)
- [ ] Google Search Console connected and verified
- [ ] Sitemap submitted to Google Search Console
- [ ] Bing Webmaster Tools connected (optional but free traffic)
- [ ] 404 errors monitored and fixed regularly
- [ ] Core Web Vitals monitored in Search Console or via RUM

## Automated Checks (Playwright)

Several SEO items can be verified automatically in your existing Playwright test suite:

```js
// Check title tag exists and has content
const title = await page.title();
expect(title).toBeTruthy();
expect(title.length).toBeGreaterThan(10);
expect(title.length).toBeLessThan(65);

// Check meta description exists
const desc = await page.$eval('meta[name="description"]', el => el.content);
expect(desc).toBeTruthy();
expect(desc.length).toBeGreaterThan(50);
expect(desc.length).toBeLessThan(165);

// Check canonical URL
const canonical = await page.$eval('link[rel="canonical"]', el => el.href);
expect(canonical).toBeTruthy();

// Check h1 exists and is unique
const h1s = await page.$$eval('h1', els => els.map(e => e.textContent));
expect(h1s.length).toBe(1);
expect(h1s[0].trim()).toBeTruthy();

// Check Open Graph tags
const ogTitle = await page.$eval('meta[property="og:title"]', el => el.content);
const ogDesc = await page.$eval('meta[property="og:description"]', el => el.content);
const ogImage = await page.$eval('meta[property="og:image"]', el => el.content);
expect(ogTitle).toBeTruthy();
expect(ogDesc).toBeTruthy();
expect(ogImage).toBeTruthy();

// Check structured data exists
const jsonLd = await page.$$eval('script[type="application/ld+json"]', els => els.length);
expect(jsonLd).toBeGreaterThan(0);

// Check lang attribute
const lang = await page.$eval('html', el => el.lang);
expect(lang).toBeTruthy();

// Check robots meta (should NOT be noindex on public pages)
const robotsMeta = await page.$('meta[name="robots"][content*="noindex"]');
expect(robotsMeta).toBeNull();
```

Add these to your existing Playwright e2e tests for public-facing pages. They run in CI alongside your UI QA checks.

## What This Skill Does NOT Cover

- **Content strategy** (what to write about, keyword research) -- this requires domain expertise and business context
- **Link building** (acquiring external backlinks) -- this is an ongoing marketing activity, not a code checklist
- **Paid search / SEM** -- this is advertising, not technical SEO
- **Competitor analysis** -- requires business context

## When to Skip This Skill

- Internal tools and admin panels (no public search visibility needed)
- APIs without a frontend
- Apps entirely behind authentication
- Mobile-only native apps (use App Store Optimization instead)
