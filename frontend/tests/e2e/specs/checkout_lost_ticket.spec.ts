import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { CheckInPage } from '../pages/CheckInPage';
import { CheckOutPage } from '../pages/CheckOutPage';
import { adminCredentials, randomPlate } from '../utils/testData';

test.describe('Check-Out - Lost Ticket', () => {
  test('should process lost ticket checkout and open gate after payment', async ({ page }) => {
    const plate = randomPlate('30LOST');

    // Login
    const loginPage = new LoginPage(page);
    await loginPage.navigate();
    await loginPage.login(adminCredentials.username, adminCredentials.password);
    await page.waitForURL(/.*dashboard/, { timeout: 5000 });

    // Check-in first
    const checkInPage = new CheckInPage(page);
    await checkInPage.navigate();
    await checkInPage.waitForPageReady();
    await checkInPage.selectVehicleType('CAR');
    await checkInPage.checkIn(plate);

    await page.waitForTimeout(1000);
    expect(await page.isVisible(`text=✅ Check-in THÀNH CÔNG: ${plate}`)).toBeTruthy();

    // Lost ticket checkout
    const checkOutPage = new CheckOutPage(page);
    await checkOutPage.navigate();
    await checkOutPage.selectMode('LOST');
    await checkOutPage.setPlate(plate);
    await checkOutPage.checkVehicle();

    // Process payment (QR -> confirm success)
    await checkOutPage.processPayment();
    await checkOutPage.verifyGateOpened();
  });
});
