import { BasePage } from './BasePage';
import { Page } from '@playwright/test';

export class LoginPage extends BasePage {
    constructor(page: Page) {
        super(page, '/');
    }

    async isLoaded(): Promise<boolean> {
        return (await this.page.isVisible('text=Đăng nhập')) && (await this.page.isVisible('text=SmartPark'));
    }

    async login(username: string, pass: string, gateLabel?: string) {
        await this.fill('input[placeholder="admin"]', username);
        await this.fill('input[placeholder="123"]', pass);

        if (gateLabel) {
            // Select Gate by text content
            await this.page.click(`button:has-text("${gateLabel}")`);
        } else {
            // Default gate is usually selected, but to be safe select the first one if needed.
            // But per code, first one is selected.
        }

        await this.click('button:has-text("Đăng nhập")');
        // Wait for navigation or failure
    }

    async getErrorMessage(): Promise<string> {
        const errorLoc = this.page.locator('.text-red-600');
        await errorLoc.waitFor({ state: 'visible', timeout: 10000 });
        return await errorLoc.textContent() || '';
    }
}
