import { BasePage } from './BasePage';
import { Page } from '@playwright/test';

export class CheckInPage extends BasePage {
    constructor(page: Page) {
        super(page, '/checkin');
    }

    async isLoaded(): Promise<boolean> {
        return await this.page.isVisible('text=Cổng Vào');
    }

    async waitForPageReady() {
        await this.page.waitForSelector('text=Cổng Vào', { state: 'visible' });
    }

    async selectVehicleType(type: 'CAR' | 'MOTORBIKE' | 'BICYCLE') {
        // Map type to full button text including emoji
        const labelMap: Record<string, string> = {
            'CAR': '🚗 Ô tô',
            'MOTORBIKE': '🛵 Xe máy',
            'BICYCLE': '🚲 Xe đạp'
        };
        const label = labelMap[type];
        if (label) {
            // Use getByRole with exact match to avoid ambiguity
            // This ensures we don't select "Ô tô điện" when looking for "Ô tô"
            await this.page.getByRole('button', { name: label, exact: true }).click();
        }
    }

    async checkIn(plate: string, isMonthly: boolean = false, cardId: string = '') {
        // Wait for the input field to be visible before filling
        await this.page.waitForSelector('input[placeholder="30A-123.45"]', { state: 'visible' });
        await this.fill('input[placeholder="30A-123.45"]', plate);

        if (isMonthly) {
            await this.click('text=Vé Tháng (Thành Viên)');
            // Wait for card input to appear
            await this.fill('input[placeholder="Quẹt thẻ thành viên..."]', cardId);
        } else {
            await this.click('text=Vé Lượt (Khách vãng lai)');
        }

        await this.click('button:has-text("XÁC NHẬN VÀO BẾN")');
    }

    async verifySuccess(plate: string): Promise<boolean> {
        // Look for log entry
        return await this.page.isVisible(`text=Check-in THÀNH CÔNG: ${plate}`);
    }

    async verifyError(msgPart: string): Promise<boolean> {
        return await this.page.isVisible(`text=${msgPart}`);
    }
}
