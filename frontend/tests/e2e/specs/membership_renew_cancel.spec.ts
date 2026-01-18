import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { MembershipPage } from '../pages/MembershipPage';
import { adminCredentials, randomPhone, randomPlate } from '../utils/testData';

test.describe('Membership - Renew & Cancel', () => {
  test('should renew and cancel a monthly ticket', async ({ page }) => {
    const plate = randomPlate('30REN');

    // Login as admin
    const loginPage = new LoginPage(page);
    await loginPage.navigate();
    await loginPage.login(adminCredentials.username, adminCredentials.password);
    await page.waitForURL(/.*dashboard/, { timeout: 5000 });

    const membershipPage = new MembershipPage(page);
    await membershipPage.navigate();

    // Accept all dialogs (confirm, prompt, alert)
    page.on('dialog', async (dialog) => {
      if (dialog.type() === 'prompt') {
        await dialog.accept('Auto cancel from E2E');
      } else {
        await dialog.accept();
      }
    });

    // Register ticket
    await membershipPage.registerTicket({
      name: 'Auto Renew',
      phone: randomPhone(),
      plate,
      identity: '001200000000',
      type: 'CAR',
      policyName: 'Vé tháng Ô tô',
      months: 1,
      brand: 'Honda',
      color: 'Black'
    });

    await membershipPage.closeQrModalIfVisible();
    await membershipPage.verifyTicketActive(plate);

    // Renew ticket
    await membershipPage.openRenewModalByPlate(plate);
    await membershipPage.submitRenew(1);
    await membershipPage.closeQrModalIfVisible();

    await membershipPage.openHistoryByPlate(plate);
    await expect(page.locator('text=Gia hạn')).toBeVisible();

    // Cancel ticket as admin
    await membershipPage.navigate();
    await membershipPage.verifyTicketActive(plate);
    await membershipPage.cancelByPlateAsAdmin(plate);

    // Verify appears in cancelled tab
    await membershipPage.openCancelledTab();
    await membershipPage.searchTicket(plate);
    await expect(page.locator(`text=${plate}`)).toBeVisible();
  });
});
