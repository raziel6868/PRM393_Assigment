import { test, expect, type APIRequestContext } from '@playwright/test';
import { QA_API_ORIGIN } from '../playwright.config';

const administratorUserName = process.env.QA_ADMIN_USERNAME;
const administratorPassword = process.env.QA_ADMIN_PASSWORD;

if (!administratorUserName || !administratorPassword) {
  throw new Error('QA_ADMIN_USERNAME and QA_ADMIN_PASSWORD are required.');
}

function api(path: string): string {
  return `${QA_API_ORIGIN}${path}`;
}

async function adminToken(request: APIRequestContext): Promise<string> {
  const response = await request.post(api('/api/v1/auth/sign-in'), {
    data: {
      emailOrUserName: administratorUserName!,
      password: administratorPassword!,
      clientType: 'web',
    },
    headers: { origin: 'http://localhost:5173' },
  });
  if (response.status() !== 200) {
    throw new Error(`Admin sign-in failed (${response.status()})`);
  }
  const body = await response.json();
  return body.accessToken as string;
}

test.describe('@web-password-help administrator queue', () => {
  test('pending request row renders and one-time password modal shows after issue', async ({ page, request }) => {
    const token = await adminToken(request);

    // Provision a teacher so we have a Pending password-help request to display.
    const marker = Date.now();
    const provisionResponse = await request.post(api('/api/v1/admin/users'), {
      data: {
        displayName: 'Giáo viên Hỗ trợ Web',
        userName: `web-ph-teacher-${marker}`,
        email: `web-ph-teacher-${marker}@example.invalid`,
        roles: ['teacher'],
      },
      headers: { origin: 'http://localhost:5173', authorization: `Bearer ${token}` },
    });
    expect(provisionResponse.status()).toBe(201);
    const provisioned = await provisionResponse.json();

    const helpResponse = await request.post(api('/api/v1/auth/password-help-requests'), {
      data: { emailOrUserName: provisioned.email ?? provisioned.userName },
      headers: { origin: 'http://localhost:5173' },
    });
    expect(helpResponse.status()).toBe(202);

    // Sign in through the portal as Administrator.
    await page.goto('/login');
    await page.getByLabel('Email hoặc mã tài khoản').fill(administratorUserName!);
    await page.getByLabel('Mật khẩu', { exact: false }).first().fill(administratorPassword!);
    await page.getByRole('button', { name: 'Đăng nhập' }).click();
    await page.waitForURL(/\/dashboard$/);

    await page.getByRole('link', { name: 'Yêu cầu hỗ trợ mật khẩu' }).click();
    await page.waitForURL(/\/password-help-requests$/);

    // The pending request row should be visible.
    const row = page.getByRole('row').filter({ hasText: provisioned.displayName });
    await expect(row).toBeVisible();

    await row.getByRole('button', { name: 'Cấp mật khẩu tạm' }).click();

    const modal = page.getByRole('dialog');
    await expect(modal).toBeVisible();
    await expect(modal.getByText(/Mật khẩu tạm/).first()).toBeVisible();

    // Ensure the temporary password value is rendered (length >= 12 expected by API contract).
    const passwordValue = await modal.locator('code').innerText();
    expect(passwordValue.length).toBeGreaterThanOrEqual(12);

    // Close modal; row should disappear once the request is resolved.
    await modal.getByRole('button', { name: 'Đã ghi nhận' }).click();
    await expect(row).toHaveCount(0);
  });
});
