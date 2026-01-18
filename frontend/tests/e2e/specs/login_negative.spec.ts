import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';

test.describe('Authentication - Negative', () => {
  test('should show error with invalid credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.navigate();
    await loginPage.login('admin', 'wrong-password');

    const errorMsg = await loginPage.getErrorMessage();
    expect(errorMsg).toContain('Đăng nhập thất bại');
  });
});
