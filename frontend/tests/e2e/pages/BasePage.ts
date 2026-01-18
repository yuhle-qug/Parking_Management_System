import { Page, Locator, expect } from '@playwright/test';
import path from 'path';

export abstract class BasePage {
    protected readonly page: Page;
    protected readonly url: string;

    constructor(page: Page, url: string) {
        this.page = page;
        this.url = url;
    }

    async navigate() {
        await this.page.goto(this.url);
    }

    async waitForLoad() {
        // networkidle is too strict for polling apps
        await this.page.waitForLoadState('domcontentloaded');
    }

    async takeScreenshot(name: string) {
        const screenshotPath = path.join('documentation', 'screenshots', `${name}.png`);
        await this.page.screenshot({ path: screenshotPath, fullPage: true });
        console.log(`Screenshot saved: ${screenshotPath}`);
    }

    protected async fill(selector: string, value: string) {
        const loc = this.page.locator(selector);
        await loc.waitFor({ state: 'visible' });
        await loc.fill(value);
    }

    protected async click(selector: string) {
        await this.page.click(selector);
    }

    protected async getText(selector: string): Promise<string> {
        return (await this.page.textContent(selector)) || '';
    }

    // Abstract check to ensure page is loaded/valid
    abstract isLoaded(): Promise<boolean>;
}
