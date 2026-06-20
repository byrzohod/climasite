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
        await Page.WaitForSelectorAsync(CartIcon, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.ClickAsync(CartIcon);

        await Page.WaitForSelectorAsync(Drawer, new PageWaitForSelectorOptions { Timeout = 10000 });
        await Page.WaitForSelectorAsync(Footer, new PageWaitForSelectorOptions { Timeout = 10000 });

        // Drawer slides in via an Angular animation; wait for the checkout action to settle into place
        // so element-at-point assertions read the final layout, not a mid-transition frame.
        await Assertions.Expect(Page.Locator(CheckoutButton)).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
    }

    public async Task<bool> IsOpenAsync() => await IsVisibleAsync(Drawer);

    /// <summary>
    /// Returns the element-tag/testid that the browser hit-tests at the centre of a drawer control.
    /// Used to prove the control — not the backdrop or the cookie banner — is the real topmost element.
    /// </summary>
    public async Task<TopmostElement> GetTopmostElementAtAsync(string selector)
    {
        var handle = await Page.WaitForSelectorAsync(selector,
            new PageWaitForSelectorOptions { Timeout = 10000 });
        if (handle is null)
        {
            throw new InvalidOperationException($"Drawer control not found: {selector}");
        }

        var box = await handle.BoundingBoxAsync()
            ?? throw new InvalidOperationException($"Drawer control has no bounding box: {selector}");

        var centerX = box.X + box.Width / 2;
        var centerY = box.Y + box.Height / 2;

        // Resolve the topmost element at the control's centre, then walk up its ancestor chain so a hit
        // on an inner <span>/<svg> still resolves to the owning [data-testid] control.
        var result = await Page.EvaluateAsync<TopmostElement>(
            @"({ x, y }) => {
                const el = document.elementFromPoint(x, y);
                if (!el) {
                    return { found: false, tag: '', testId: '', matchedTestId: '' };
                }
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
            new { x = centerX, y = centerY });

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
