import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { ReportPage } from '../pages/ReportPage';

test.describe('Reports & Analytics', () => {
    test.beforeEach(async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.navigate();
        await loginPage.login('admin', '123'); // Admin only
        await page.waitForURL(/.*dashboard/, { timeout: 5000 });
    });

    test('should generate revenue and traffic reports', async ({ page }) => {
        const reportPage = new ReportPage(page);
        await reportPage.navigate();
        expect(await reportPage.isLoaded()).toBeTruthy();

        // Set range (e.g., last 30 days)
        // Format YYYY-MM-DD
        const today = new Date();
        const past = new Date();
        past.setDate(today.getDate() - 30);

        const fmt = (d: Date) => d.toISOString().split('T')[0];

        await reportPage.selectDateRange(fmt(past), fmt(today));
        await reportPage.generateReport();

        await reportPage.verifyChartsVisible();
        await reportPage.takeScreenshot('reports-generated');
    });
});
