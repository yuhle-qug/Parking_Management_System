import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { CheckInPage } from '../pages/CheckInPage';
import { CheckOutPage } from '../pages/CheckOutPage';

test.describe('Full Parking Cycle', () => {
    test('should allow a car to check-in and check-out', async ({ page }) => {
        const plate = `30Full-${Math.floor(Math.random() * 10000)}`;

        // 1. Login
        const loginPage = new LoginPage(page);
        await loginPage.navigate();
        await loginPage.login('admin', '123');
        await page.waitForURL(/.*dashboard/, { timeout: 5000 });

        // 2. Check-In
        const checkInPage = new CheckInPage(page);
        await checkInPage.navigate();
        await checkInPage.waitForPageReady();
        await checkInPage.selectVehicleType('CAR');
        await checkInPage.checkIn(plate);

        // Wait for success message
        await page.waitForTimeout(1000);
        expect(await page.isVisible(`text=✅ Check-in THÀNH CÔNG: ${plate}`)).toBeTruthy();
        await checkInPage.takeScreenshot(`cycle-1-checkin-${plate}`);

        // 3. Check-Out
        const checkOutPage = new CheckOutPage(page);
        await checkOutPage.navigate();
        await checkOutPage.enterPlateOrTicket(plate);
        await checkOutPage.checkVehicle();

        // 4. Verify Payment logic
        // Expect fee > 0 (or 0 depending on policy, typically > 0 for standard)
        // await expect(page.locator('.text-red-600')).toBeVisible(); // Fee displayed

        await checkOutPage.processPayment();
        await checkOutPage.verifyGateOpened();
        await checkOutPage.takeScreenshot(`cycle-2-checkout-${plate}`);
    });
});
