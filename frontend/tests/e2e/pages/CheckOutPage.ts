import { BasePage } from './BasePage';
import { Page, expect } from '@playwright/test';

export class CheckOutPage extends BasePage {
    constructor(page: Page) {
        super(page, '/checkout');
    }

    async isLoaded(): Promise<boolean> {
        return await this.page.isVisible('text=Cổng Ra');
    }

    async enterPlateOrTicket(value: string, isMonthly: boolean = false, isLost: boolean = false) {
        // Mode selection
        if (isLost) {
            await this.click('text=Mất Vé');
        } else if (isMonthly) {
            await this.click('text=Vé Tháng');
            await this.fill('input[placeholder="Quẹt thẻ..."]', value);
        } else {
            // Vé Lượt default
            await this.click('text=Vé Lượt');
            // Try filling Ticket ID if available, else Plate
            // The UI has Conditional Inputs.
            // If "Vé Lượt", checks "ticketId" input or "plate".
            // Input placeholder "Nhập mã vé..."
            const ticketInput = this.page.locator('input[placeholder="Nhập mã vé..."]');
            if (await ticketInput.isVisible()) {
                await ticketInput.fill(value);
            } else {
                // Fallback to Plate Input "30A-..."
                await this.fill('input[placeholder="30A-..."]', value);
            }
        }
    }

    async setPlate(plate: string) {
        await this.page.getByPlaceholder('30A-...').fill(plate);
    }

    async selectMode(mode: 'SINGLE' | 'MONTHLY' | 'LOST') {
        if (mode === 'MONTHLY') {
            await this.click('text=Vé Tháng');
        } else if (mode === 'LOST') {
            await this.click('text=Mất Vé');
        } else {
            await this.click('text=Vé Lượt');
        }
    }

    async checkVehicle() {
        await this.click('button:has-text("KIỂM TRA XE RA")');
        // Wait for result or error
    }

    async getFeeAmount(): Promise<string> {
        // Locate "Tổng thanh toán" -> sibling value
        return await this.getText('.text-red-600');
    }

    async processPayment() {
        // Check if "MIỄN PHÍ" or "Vé Tháng" (Free)
        if (await this.page.isVisible('text=MỞ CỔNG NGAY')) {
            await this.click('button:has-text("MỞ CỔNG NGAY")');
            return;
        }

        // Regular Payment
        if (await this.page.isVisible('text=THANH TOÁN QR')) {
            await this.click('text=THANH TOÁN QR');
            // Wait for QR Modal or Simulation buttons
            // In the code, simulation buttons appear after QR is generated
            // "Đã nhận tiền"
            await this.page.waitForSelector('text=Đã nhận tiền');
            await this.click('text=Đã nhận tiền');
        }
    }

    async verifyGateOpened() {
        // Log "Đã mở cổng"
        await expect(this.page.getByText(/Đã mở cổng/)).toBeVisible({ timeout: 15000 });
    }
}
