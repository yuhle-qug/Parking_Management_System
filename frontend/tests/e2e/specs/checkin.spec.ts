import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { CheckInPage } from '../pages/CheckInPage';

test.describe('Authentication', () => {
    let loginPage: LoginPage;

    test.beforeEach(async ({ page }) => {
        loginPage = new LoginPage(page);
        await loginPage.navigate();
    });

    test('should login successfully with valid credentials', async ({ page }) => {
        await loginPage.login('admin', '123'); // Assuming default admin/123
        // Verify redirect to Dashboard
        await expect(page).toHaveURL(/.*dashboard/);
    });
});

test.describe('Check-In Flow', () => {
    let checkInPage: CheckInPage;

    test.beforeEach(async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.navigate();
        await loginPage.login('admin', '123');
        // Wait for redirect after login
        await page.waitForURL(/.*dashboard/, { timeout: 5000 });
        checkInPage = new CheckInPage(page);
        await checkInPage.navigate();
    });

    test('should check-in a standard car successfully', async ({ page }) => {
        // Use a random plate to avoid "Already In" error if backend state persists
        const plate = `30Test-${Math.floor(Math.random() * 10000)}`;

        // Wait for page to be fully loaded
        await checkInPage.waitForPageReady();

        await checkInPage.selectVehicleType('CAR');
        await checkInPage.checkIn(plate);

        // Wait for success message in logs
        await page.waitForTimeout(1000);
        const success = await page.isVisible(`text=✅ Check-in THÀNH CÔNG: ${plate}`);
        expect(success).toBeTruthy();

        await checkInPage.takeScreenshot(`checkin-success-${plate}`);
    });
});
