import { BasePage } from './BasePage';
import { Page, expect } from '@playwright/test';

export class ReportPage extends BasePage {
    constructor(page: Page) {
        super(page, '/report');
    }

    async isLoaded(): Promise<boolean> {
        return await this.page.isVisible('text=Báo cáo & Thống kê');
    }

    async selectDateRange(start: string, end: string) {
        // First date input
        const inputs = this.page.locator('input[type="date"]');
        await inputs.first().waitFor({ state: 'visible' });
        await inputs.first().fill(start);
        // Second date input
        await inputs.nth(1).fill(end);
    }

    async generateReport() {
        await this.click('button:has-text("Làm mới")');
        // Wait for loading to finish
        // Loading state adds 'animate-spin' to icon inside button. 
        // We can wait for it to disappear or just wait for network idle.
        await this.waitForLoad();
    }

    async verifyChartsVisible() {
        // Recharts creates .recharts-wrapper
        await expect(this.page.locator('.recharts-wrapper').first()).toBeVisible();
    }
}
