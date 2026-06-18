using Microsoft.Playwright;

namespace ClimaSite.E2E.PageObjects;

public class HomePage : BasePage
{
    private const string SearchInput = "[data-testid='search-input']";
    private const string SearchButton = "[data-testid='search-button']";
    private const string FeaturedProducts = "[data-testid^='home-v3-rec-card-']";
    private const string CategoryCard = "[data-testid^='home-v3-cat-']";
    private const string LoginButton = "[data-testid='login-button']";
    private const string CartIcon = "[data-testid='cart-icon']";
    private const string CartCount = "[data-testid='cart-count']";
    private const string LanguageSelector = "[data-testid='language-selector']";
    private const string ThemeToggle = "[data-testid='theme-toggle']";

    public HomePage(IPage page) : base(page) { }

    public async Task NavigateAsync()
    {
        await Page.GotoAsync("/");
        await WaitForLoadAsync();
    }

    public async Task SearchAsync(string query)
    {
        await FillAsync(SearchInput, query);
        await ClickAsync(SearchButton);
        await WaitForLoadAsync();
    }

    public async Task GoToLoginAsync()
    {
        await ClickAsync(LoginButton);
        await WaitForLoadAsync();
    }

    public async Task GoToCartAsync()
    {
        await ClickAsync(CartIcon);
        await WaitForLoadAsync();
    }

    public async Task<int> GetCartCountAsync()
    {
        try
        {
            // Wait briefly for cart count badge (only appears when count > 0)
            await Page.WaitForSelectorAsync(CartCount, new PageWaitForSelectorOptions { Timeout = 3000 });
            var countText = await GetTextAsync(CartCount);
            return int.TryParse(countText, out var count) ? count : 0;
        }
        catch (TimeoutException)
        {
            // Cart badge not visible means cart is empty
            return 0;
        }
    }

    public async Task SelectLanguageAsync(string languageCode)
    {
        // Hover on the language selector container to open dropdown (uses mouseenter event)
        var langSelector = Page.Locator("[data-testid='language-selector']");
        await langSelector.HoverAsync();
        // Wait for dropdown to appear
        await Page.WaitForSelectorAsync("[data-testid='language-dropdown']", new PageWaitForSelectorOptions { Timeout = 5000 });
        // Click on the specific language option
        await Page.ClickAsync($"[data-testid='language-{languageCode}']");
        await WaitForLoadAsync();
    }

    public async Task ToggleThemeAsync()
    {
        await ClickAsync(ThemeToggle);
    }

    public async Task<bool> HasFeaturedProductsAsync()
    {
        try
        {
            await WaitForSelectorAsync(FeaturedProducts, 15000);
            return await IsVisibleAsync(FeaturedProducts);
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public async Task<int> WaitForRecommendationCountAsync()
    {
        await WaitForSelectorAsync(FeaturedProducts, 15000);
        return await Page.Locator(FeaturedProducts).CountAsync();
    }

    public async Task<int> GetCategoryCountAsync()
    {
        // Home below-fold content is deferred; wait for at least one category to render
        // before counting (callers expect >=1 category).
        await WaitForSelectorAsync(CategoryCard, 15000);
        var categories = await Page.QuerySelectorAllAsync(CategoryCard);
        return categories.Count;
    }
}
