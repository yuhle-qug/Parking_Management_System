import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { MembershipPage } from '../pages/MembershipPage';
import { CheckInPage } from '../pages/CheckInPage';
import { adminCredentials, randomPhone, randomPlate } from '../utils/testData';

test.describe('Check-In - Monthly Ticket', () => {
  test('should allow monthly ticket check-in with card id', async ({ page }) => {
    const plate = randomPlate('30MEM');

    // Login
    const loginPage = new LoginPage(page);
    await loginPage.navigate();
    await loginPage.login(adminCredentials.username, adminCredentials.password);
    await page.waitForURL(/.*dashboard/, { timeout: 5000 });

    // Register monthly ticket
    const membershipPage = new MembershipPage(page);
    await membershipPage.navigate();

    page.on('dialog', async (dialog) => {
      await dialog.accept();
    });

    await membershipPage.registerTicket({
      name: 'Auto Test',
      phone: randomPhone(),
      plate,
      identity: '001200000000',
      type: 'CAR',
      policyName: 'Vé tháng Ô tô',
      months: 1,
      brand: 'Toyota',
      color: 'Blue'
    });

    await membershipPage.closeQrModalIfVisible();
    await membershipPage.verifyTicketActive(plate);

    const ticketId = await membershipPage.getTicketIdByPlate(plate);
    expect(ticketId).toBeTruthy();

    // Check-in with monthly card
    const checkInPage = new CheckInPage(page);
    await checkInPage.navigate();
    await checkInPage.waitForPageReady();
    await checkInPage.selectVehicleType('CAR');
    await checkInPage.checkIn(plate, true, ticketId);

    await page.waitForTimeout(1000);
    const success = await page.isVisible(`text=✅ Check-in THÀNH CÔNG: ${plate}`);
    expect(success).toBeTruthy();
  });
});
