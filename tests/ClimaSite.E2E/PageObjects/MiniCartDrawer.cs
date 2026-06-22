using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

/// <summary>
/// Page object for the header mini-cart drawer (SLICE D). The drawer is opened by clicking the
/// header cart icon on desktop and exposes "view cart" + "checkout" actions. These actions are the
/// surface that the reported cart/checkout overlay bug lived on: the checkout link must be the real
/// topmost element, not covered by the drawer backdrop or the cookie-consent banner.
/// </summary>
public class MiniCartDrawer : BasePage
{
    public const string CartIcon = "[data-testid='cart-icon']";
    public const string Drawer = "[data-testid='mini-cart-drawer']";
    public const string Backdrop = "[data-testid='mini-cart-backdrop']";
    public const string Footer = "[data-testid='mini-cart-footer']";
    public const string CheckoutButton = "[data-testid='mini-cart-checkout']";
    public const string ViewCartButton = "[data-testid='mini-cart-view-cart']";
    public const string CloseButton = "[data-testid='mini-cart-close']";

    public MiniCartDrawer(IPage page) : base(page) { }

    /// <summary>
    /// Clicks the header cart icon (desktop viewport opens the drawer) and waits for the drawer panel
    /// plus its footer actions to render. The footer only renders when the cart has items.
    /// </summary>
    public async Task OpenAsync()
    {
        await Page.WaitForSelectorAsync(CartIcon, new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.ClickAsync(CartIcon);

        await Page.WaitForSelectorAsync(Drawer, new PageWaitForSelectorOptions { Timeout = 30000 });
        await Page.WaitForSelectorAsync(Footer, new PageWaitForSelectorOptions { Timeout = 30000 });

        // Drawer slides in via an Angular transform animation; wait for the checkout action to settle
        // into its final, on-screen position so element-at-point assertions read the settled layout,
        // not a mid-transition (possibly off-screen) frame.
        await Assertions.Expect(Page.Locator(CheckoutButton)).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 30000 });
        await WaitForSettledOnScreenAsync(CheckoutButton);
    }

    public async Task<bool> IsOpenAsync() => await IsVisibleAsync(Drawer);

    /// <summary>
    /// Waits until the control has a finite, non-zero bounding box whose centre is inside the viewport.
    /// The drawer animates in with a CSS transform, so a too-early read can produce an off-screen or
    /// non-finite point (which makes document.elementFromPoint throw "non-finite value").
    /// </summary>
    private async Task WaitForSettledOnScreenAsync(string selector)
    {
        await Page.WaitForFunctionAsync(
            @"(sel) => {
                const el = document.querySelector(sel);
                if (!el) return false;
                const r = el.getBoundingClientRect();
                if (!r || !isFinite(r.left) || !isFinite(r.top) || r.width <= 0 || r.height <= 0) return false;
                const cx = r.left + r.width / 2;
                const cy = r.top + r.height / 2;
                return cx >= 0 && cy >= 0 && cx <= window.innerWidth && cy <= window.innerHeight;
            }",
            selector,
            new PageWaitForFunctionOptions { Timeout = 30000 });
    }

    /// <summary>
    /// Returns the element-tag/testid that the browser hit-tests at the centre of a drawer control.
    /// Used to prove the control — not the backdrop or the cookie banner — is the real topmost element.
    /// The coordinate is computed and hit-tested entirely in the browser (from getBoundingClientRect),
    /// guarded against non-finite values, so it never feeds a NaN point to elementFromPoint.
    /// </summary>
    public async Task<TopmostElement> GetTopmostElementAtAsync(string selector)
    {
        await WaitForSettledOnScreenAsync(selector);

        var result = await Page.EvaluateAsync<TopmostElement>(
            @"(sel) => {
                const target = document.querySelector(sel);
                if (!target) {
                    return { found: false, tag: '', testId: '', matchedTestId: '' };
                }
                const r = target.getBoundingClientRect();
                const x = Math.round(r.left + r.width / 2);
                const y = Math.round(r.top + r.height / 2);
                if (!isFinite(x) || !isFinite(y)) {
                    return { found: false, tag: '', testId: '', matchedTestId: '' };
                }
                const el = document.elementFromPoint(x, y);
                if (!el) {
                    return { found: false, tag: '', testId: '', matchedTestId: '' };
                }
                // Walk up the ancestor chain so a hit on an inner <span>/<svg> still resolves to the
                // owning [data-testid] control.
                let node = el;
                let matchedTestId = '';
                while (node) {
                    const tid = node.getAttribute && node.getAttribute('data-testid');
                    if (tid) { matchedTestId = tid; break; }
                    node = node.parentElement;
                }
                return {
                    found: true,
                    tag: el.tagName.toLowerCase(),
                    testId: (el.getAttribute && el.getAttribute('data-testid')) || '',
                    matchedTestId
                };
            }",
            selector);

        return result;
    }
}

/// <summary>Result of an element-from-point hit test (see <see cref="MiniCartDrawer"/>).</summary>
public class TopmostElement
{
    public bool Found { get; set; }
    public string Tag { get; set; } = string.Empty;

    /// <summary>data-testid of the exact hit element (may be empty for inner svg/span).</summary>
    public string TestId { get; set; } = string.Empty;

    /// <summary>Nearest data-testid walking up from the hit element (the owning control).</summary>
    public string MatchedTestId { get; set; } = string.Empty;
}
