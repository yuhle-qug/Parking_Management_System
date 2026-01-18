import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';
import { MembershipPage } from '../pages/MembershipPage';

test.describe('Membership Management', () => {
    test.beforeEach(async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.navigate();
        await loginPage.login('admin', '123');
        await page.waitForURL(/.*dashboard/, { timeout: 5000 });
    });

    test('should register a new monthly ticket', async ({ page }) => {
        const membershipPage = new MembershipPage(page);
        await membershipPage.navigate();

        const plate = `30Mem-${Math.floor(Math.random() * 10000)}`;
        const data = {
            name: 'Test User',
            phone: '0909999999',
            plate: plate,
            identity: '001200000000',
            type: 'CAR' as const,
            policyName: 'Vé tháng Ô tô', // From defaultPolicies in Membership.jsx line 59
            months: 1,
            brand: 'Toyota',
            color: 'Red'
        };

        // Handle success dialog
        membershipPage.handleDialog('thành công');

        await membershipPage.registerTicket(data);

        // Active tab check
        await membershipPage.verifyTicketActive(plate);
        await membershipPage.takeScreenshot(`membership-registered-${plate}`);
    });
});
