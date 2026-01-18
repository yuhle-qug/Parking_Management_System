import { BasePage } from './BasePage';
import { Page, expect } from '@playwright/test';

export interface MembershipData {
    name: string;
    phone: string;
    plate: string;
    identity: string;
    type: 'CAR' | 'MOTORBIKE' | 'BICYCLE';
    policyName: string;
    months: number;
    brand?: string;
    color?: string;
}

export class MembershipPage extends BasePage {
    constructor(page: Page) {
        super(page, '/membership');
    }

    async isLoaded(): Promise<boolean> {
        return await this.page.isVisible('text=Đăng ký vé tháng');
    }

    async registerTicket(data: MembershipData) {
        await this.page.waitForSelector('text=Đăng ký vé tháng', { state: 'visible' });
        await this.page.waitForSelector('input[placeholder="Nguyễn Văn A"]', { state: 'visible' });
        await this.page.waitForSelector(`button:has-text("${data.policyName}")`, { state: 'visible' });
        await this.fill('input[placeholder="Nguyễn Văn A"]', data.name);
        await this.fill('input[placeholder="0901234567"]', data.phone);
        await this.fill('input[placeholder="30A-12345"]', data.plate);
        await this.fill('input[placeholder="0123456789"]', data.identity);

        // Select vehicle type by value (first select element on the page)
        // The select has options: CAR, MOTORBIKE, ELECTRIC_CAR, ELECTRIC_MOTORBIKE, BICYCLE
        await this.page.locator('select').first().selectOption(data.type);

        if (data.brand) await this.fill('input[placeholder="Toyota, Honda..."]', data.brand);
        if (data.color) await this.fill('input[placeholder="Đen, Trắng..."]', data.color);

        // Policy - Click button containing policy name
        await this.click(`button:has-text("${data.policyName}")`);

        // Months - Use last() to get the duration select (first is vehicle type)
        await this.page.locator('select').last().selectOption(data.months.toString());

        // Submit
        await this.click('text=Đăng ký ngay');

        // Confirm Modal
        await this.click('button:has-text("Xác nhận")');

        // Wait for success alert/modal closure
        // Note: Code uses `alert()` -> Playwright must handle dialog.
        // Or maybe it shows a simulation modal? Code: `alert(...)`.
        // Wait, line 198: `alert('Đăng ký thành công...')`.
        // I need to handle dialog in test.
    }

    async handleDialog(expectedMessage: string) {
        this.page.once('dialog', async dialog => {
            console.log(`Dialog message: ${dialog.message()}`);
            if (dialog.message().includes(expectedMessage)) {
                await dialog.accept();
            } else {
                await dialog.dismiss();
            }
        });
    }

    async verifyTicketActive(plate: string) {
        // Switch to Active Tab
        await this.click('text=Đang hoạt động');
        // Search
        await this.fill('input[placeholder="Tìm mã vé / biển số"]', plate);
        // Verify card exists
        await expect(this.page.locator(`text=${plate}`)).toBeVisible();
    }

    async searchTicket(plate: string) {
        await this.fill('input[placeholder="Tìm mã vé / biển số"]', plate);
    }

    private ticketCardByPlate(plate: string) {
        return this.page
            .locator('div', { hasText: plate })
            .filter({ has: this.page.locator('button:has-text("Lịch sử")') })
            .first();
    }

    async getTicketIdByPlate(plate: string): Promise<string> {
        const card = this.ticketCardByPlate(plate);
        await expect(card).toBeVisible();
        const ticketId = await card.locator('span.font-mono').first().textContent();
        return (ticketId || '').trim();
    }

    async openRenewModalByPlate(plate: string) {
        const card = this.ticketCardByPlate(plate);
        await expect(card).toBeVisible();
        await card.locator('button:has-text("Gia hạn")').click();
        await expect(this.page.locator('text=Gia hạn vé tháng')).toBeVisible();
    }

    async submitRenew(months: number) {
        const modal = this.page.locator('div').filter({ hasText: 'Gia hạn vé tháng' }).first();
        await modal.locator('select').first().selectOption(months.toString());
        await this.click('button:has-text("Xác nhận gia hạn")');
    }

    async requestCancelByPlate(plate: string) {
        const card = this.ticketCardByPlate(plate);
        await expect(card).toBeVisible();
        await card.locator('button:has-text("Yêu cầu hủy")').click();
    }

    async cancelByPlateAsAdmin(plate: string) {
        const card = this.ticketCardByPlate(plate);
        await expect(card).toBeVisible();
        await card.locator('button:has-text("Hủy vé")').click();
    }

    async openCancelledTab() {
        await this.click('text=Đã hủy');
    }

    async openHistoryByPlate(plate: string) {
        const card = this.ticketCardByPlate(plate);
        await expect(card).toBeVisible();
        await card.locator('button:has-text("Lịch sử")').click();
        await expect(this.page.locator('text=Lịch sử vé tháng')).toBeVisible();
    }

    async closeQrModalIfVisible() {
        const closeBtn = this.page.locator('button:has-text("Đóng")');
        if (await closeBtn.isVisible()) {
            await closeBtn.click();
        }
    }
}
